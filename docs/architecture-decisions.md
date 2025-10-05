# Architecture Decisions: Event Sourcing with Marten

## Core Philosophy

**Event Sourcing as Default, CRUD with Justification**

### Why ES-First?

1. **Data Loss Prevention**: `UPDATE users SET email = ?` without WHERE → recoverable with ES, catastrophic with CRUD
2. **Time Travel Debugging**: Replay state at any point in time for production debugging
3. **Implicit Audit**: Every change is captured without additional overhead
4. **AI/ML Readiness**: Behavioral data captured preemptively for future analytics
5. **Team Skill Building**: Consistency breeds expertise (not siloed "complex domain" knowledge)

### GenAI Era Advantage

With Claude/Copilot, the boilerplate overhead of ES is eliminated:
- CRUD: 3 minutes, 8 LOC, no validation/audit
- ES with AI: 4 minutes, 35 LOC, validation + audit + rollback + tests
- **Delta: +1 minute for 10x capability**

## Technology Stack

- **Event Store**: Marten 8.11 (PostgreSQL-based)
- **Command Handling**: Scoped services with transactional sessions
- **Framework**: ASP.NET Core 9.0
- **Projections**: Inline (strong consistency) and Async (eventual consistency)
- **Session Activity Tracking**: CRUD table (non-event-sourced) for performance

## Architectural Patterns

### 1. Command Service Pattern

```csharp
public class UserCommandService : CommandServiceBase
{
    public UserCommandService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<CommandResult> CreateUser(CreateUserCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            // Validate business rules
            var existingUser = await session.Query<User>()
                .FirstOrDefaultAsync(u => u.UserName == cmd.UserName);

            if (existingUser != null)
                return new CommandResult(cmd, false, null, "Username exists");

            // Append event
            var userId = GuidProvider.NewGuid();
            session.Events.StartStream<User>(userId,
                new UserCreatedEvent(sessionObj.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
        });
    }
}
```

**Key Learnings:**
- ✅ Inherit from `CommandServiceBase` for DI and helper methods
- ✅ Use `ExecuteCommandInSession()` for automatic session validation and activity tracking
- ✅ Services are scoped, one instance per HTTP request
- ✅ Transactional: changes auto-commit or rollback on exception

### 2. Projection Pattern

```csharp
public class SomethingCounterProjection : SingleStreamProjection<SomethingCounter, Guid>
{
    public static SomethingCounter Create(SomethingHappened started) =>
        new(started.Id, 1);

    public static SomethingCounter Apply(SomethingCounter counter, SomethingHappened happened) =>
        counter with { Count = counter.Count + 1 };
}
```

**Key Learnings:**
- ✅ Use `SingleStreamProjection<TDoc, TId>` for aggregate projections
- ✅ Register: `options.Projections.Add<T>(ProjectionLifecycle.Inline)`

### 3. Controller Integration

```csharp
public class UserController : V1CommandControllerBase
{
    private readonly UserCommandService _service;

    public UserController(UserCommandService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserCmd cmd) // SessionId auto-injected
    {
        var result = await _service.CreateUser(cmd);
        return result.Success
            ? Ok(new { UserId = (Guid)result.ResultData })
            : StatusCode(500, new { result.ErrorMessage });
    }
}

public class UserQueryController : V1QueryControllerBase
{
    private readonly IDocumentStore _store;

    public UserQueryController(IDocumentStore store)
    {
        _store = store;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        await using var session = _store.LightweightSession();
        var user = await session.LoadAsync<User>(userId);
        return user != null ? Ok(user) : NotFound();
    }
}
```

**Key Learnings:**
- ✅ Inject command services directly (scoped lifetime)
- ✅ Commands accepted via `[FromBody]` with auto SessionId injection via `CmdModelBinder`
- ✅ Inject `IDocumentStore` (not `IDocumentSession`) for queries
- ✅ Create lightweight sessions per query

### 4. DI Registration (Program.cs)

```csharp
// Marten
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<SessionProjection>(ProjectionLifecycle.Inline);
})
.UseLightweightSessions();

// Command Services
builder.Services.AddScoped<UserCommandService>();
builder.Services.AddScoped<SessionCommandService>();
builder.Services.AddScoped<RebuildProjectionService>();

// Model Binder for SessionId auto-injection
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new CmdModelBinderProvider());
});
```

**Key Learnings:**
- ✅ Register projections at startup
- ✅ Command services are scoped (one per HTTP request)
- ✅ `CmdModelBinder` automatically injects SessionId from HttpContext into commands

## Operational Patterns

### Projection Rebuild

```csharp
[HttpPost("rebuild")]
public async Task<IActionResult> RebuildProjections([FromBody] RebuildProjectionCommand cmd)
{
    var result = await _rebuildService.RebuildProjection(cmd);
    return result.Success
        ? Ok(new { Message = result.ResultData })
        : StatusCode(500, new { result.ErrorMessage });
}
```

**Use Cases:**
- Projection logic changed → rebuild from events
- Schema evolution → reprocess all streams
- Bug in projection → fix + rebuild

## When to Use CRUD (Escape Hatches)

CRUD requires **explicit justification**:

- [ ] External system expects REST PUT/DELETE
- [ ] Throwaway prototype (< 1 month lifespan)
- [ ] No business logic (pure key-value storage)
- [ ] Read-only analytics database (no writes)
- [ ] Session tokens, cache entries, feature flags

**Document CRUD decisions in ADRs (Architecture Decision Records).**

## Common Pitfalls

### ❌ Projection Issues
- **Inline projections don't rebuild**: Delete projection docs manually or use `RebuildProjectionService`

### ❌ Service Issues
- **Injecting IDocumentSession**: Wrong scope → use `IServiceProvider` in `CommandServiceBase`
- **Not using ExecuteCommandInSession**: Session validation bypassed → security issue

### ❌ DI Issues
- **Singleton services**: Command services must be scoped, not singleton

## Data Value Proposition

### ES = Optionality for AI/ML

Every event is potential training data:

```csharp
// "EmailUpdated" captures:
- Timestamp (when)
- Old value → New value (what changed)
- Triggered by (who/what)
- Correlation ID (context)

// Future use cases (without planning ahead):
- "Users who update email within 24h of signup are 3x more likely to churn"
- "Email domains ending in .edu get 20% more coupon usage"
- "Support chatbot: 'I see you changed your email yesterday. Is this about...?'"
```

**CRUD loses this forever.** ES captures it by default.

## Production Readiness: Security & Observability

### Security Implementation

**Password Hashing** (✅ Implemented)
```csharp
// ❌ NEVER use plain SHA256
var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

// ✅ Always use BCrypt with appropriate work factor
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
```

**Input Validation** (✅ Implemented)
```csharp
public class CreateUserCmdValidator : AbstractValidator<CreateUserCmd>
{
    public CreateUserCmdValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .Matches("^[a-zA-Z0-9_-]+$");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

**Error Handling** (✅ Implemented)
- Generic messages to users: "An error occurred processing your request"
- Detailed logging for developers: Full stack trace + context
- Structured logging with Serilog for easy debugging

### Performance Optimization

**Session Loading** (✅ Implemented)
- Middleware loads session once and stores in `HttpContext.Items["Session"]`
- CommandServiceBase retrieves from HttpContext (avoids duplicate DB query)
- Falls back to DB query in test scenarios (when HttpContext unavailable)
- **Performance gain**: 20-50ms per command (typical PostgreSQL query time)

**Structured Logging** (✅ Implemented)
- Serilog with console sink for development
- JSON output for production log aggregation
- Command execution logged with: CommandName, SessionId, ExecutionTime, Success/Failure
- HTTP request logging via `UseSerilogRequestLogging()`

### Observability

**Logging Strategy**
```csharp
Logger.LogInformation("Executing command {CommandName} in session {SessionId}", commandName, cmd.SessionId);
Logger.LogWarning("Command {CommandName} failed: {ErrorMessage}", commandName, result.ErrorMessage);
Logger.LogError(ex, "Unexpected error executing command {CommandName}", commandName);
```

**Metrics** (TODO)
- [ ] Command execution duration histogram
- [ ] Success/failure rate per command type
- [ ] Projection rebuild duration
- [ ] Event store write throughput

## Next Steps: Template Creation

### Completed ✅
- [x] Security: BCrypt password hashing
- [x] Security: Input validation with FluentValidation
- [x] Security: Structured error handling
- [x] Performance: Session loading from HttpContext
- [x] Observability: Serilog structured logging
- [x] Documentation: Updated CLAUDE.md + Scaffolding.md

### TODO List

1. **Cleanup Prototype**
   - [ ] Consistent namespaces and XML docs
   - [ ] Add integration tests
   - [ ] Add authorization pattern example

2. **Documentation**
   - [ ] Onboarding guide: "ES for New Devs"
   - [ ] Pattern library: CommandService + Projection + Controller
   - [ ] AI prompt templates for Claude

3. **Scaffolding**
   - [ ] `dotnet new` templates for aggregates/projections
   - [ ] Script: `new-command.sh <DomainName> <CommandName>`
   - [ ] Template files for Command/Event/Validator

4. **Tooling Investment**
   - [ ] Event browser UI (Marten dashboard or custom)
   - [ ] Projection lag monitoring (Grafana)
   - [ ] Replay scripts for production fixes
   - [ ] Schema registry for event versioning

5. **Operations**
   - [ ] Docker Compose: Postgres + Seq (logging) + Grafana
   - [ ] CI/CD pipeline for deployments
   - [ ] Runbooks: "Projection behind", "Rebuild failed"

### Recommended Project Structure

```
YourCompany.Template/
├── DomainName/              # Business domain (e.g., UserManagement)
│   ├── Aggregate.cs         # Domain aggregate + projection
│   ├── CommandService.cs    # Business logic service
│   └── Cmd/
│       └── CommandName.cs   # Command + Event records
├── Controllers/
│   ├── v1/
│   │   ├── cmd/            # Write endpoints
│   │   └── qry/            # Read endpoints
│   └── auth/               # Auth endpoints
├── Infrastructure/
│   ├── Middleware/         # SessionValidationMiddleware
│   └── ModelBinders/       # CmdModelBinder
└── EventSourcing/
    └── CommandServiceBase.cs
```

## Decision Framework

### Is ES Appropriate for This Feature?

```
┌─────────────────────────────────────┐
│ Does it have business logic?       │ Yes → ES
│ Do we need audit trail?            │ Yes → ES
│ Could we use historical data later?│ Yes → ES
│ Is it external CRUD API?           │ Yes → CRUD (with adapter)
│ Is it <1 month prototype?          │ Maybe CRUD (with migration plan)
└─────────────────────────────────────┘

Default: ES
Exception: Requires ADR with justification
```

## Strategic Advantage

**In the GenAI era, systems without behavioral history are at a competitive disadvantage.**

Event Sourcing is not just about audit trails—it's about **building AI-ready infrastructure by default**.

---

*Last Updated: 2025-10-03*
*Authors: Architecture Team + Claude*
