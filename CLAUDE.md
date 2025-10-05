# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CQRS Event Sourcing system using Marten (PostgreSQL-based event store) with Akka.NET actors for command handling. ASP.NET Core 9.0 API with strict command/query separation.

**Philosophy**: Event Sourcing by default, CRUD only with explicit justification (see docs/architecture-decisions.md for rationale).

## Development Commands

### Build & Run
```bash
# Build solution
dotnet build

# Run API (from repository root)
dotnet run --project MartenAkkaTests.Api/MartenAkkaTests.Api.csproj

# Run tests
dotnet test

# Run single test class
dotnet test --filter FullyQualifiedName~CreateSessionCmdHandlerTests
```

### Database
- **Connection**: PostgreSQL on `localhost:5435` (user: postgres, password: postgres, db: eventsourcing)
- **Schema**: Auto-created by Marten on startup
- **Reset**: Drop database and restart API to rebuild

### Swagger/API Testing
- **Development URL**: https://localhost:7XXX (check console output)
- **Swagger UI**: Available at `/swagger` with three API groups:
  - `/swagger/v1-commands/swagger.json` - Write operations
  - `/swagger/v1-queries/swagger.json` - Read operations
  - `/swagger/infrastructure/swagger.json` - Auth endpoints
- **Authentication**: Click "Authorize" in Swagger UI (creates session automatically)

## Architecture

### CQRS Command Flow

**Controller → CmdRouter → CmdHandler → Events → Projection**

1. **Controller** (`api/v1/cmd/*`): Receives HTTP request, extracts SessionId from auth header, calls CmdRouter via `Ask<CommandResult>`
2. **CmdRouter**: Routes commands to appropriate CmdHandler (creates child actor per command)
3. **CmdHandler**: Validates business rules, appends events to Marten stream, returns `CommandResult`
4. **Projection**: Marten inline projection rebuilds aggregate state from events

### Query Flow

**Controller → DocumentStore → Projection (read model)**

Query controllers inject `IDocumentStore` and create lightweight sessions to query projections. Never mutate state on query side.

### File Organization by Domain

Each business domain (UserManagement, SessionManagement) follows this structure:

```
DomainName/
├── DomainAggregate.cs          # Record + Projection (e.g., User, Session)
├── DomainManagementCmdRouter.cs  # Routes commands to handlers
└── Cmd/
    ├── CommandName.cs          # Contains 3 classes:
    │   ├── CommandNameCmd      # Command record (implements ICmd)
    │   ├── CommandNameEvent    # Event record (implements IDomainEvent)
    │   └── CommandNameCmdHandler  # Actor handling business logic
```

**Example**: `UserManagement/Cmd/CreateUser.cs` contains `CreateUserCmd`, `UserCreatedEvent`, and `CreateUserCmdHandler`.

### Key Base Classes

- **`CmdHandlerBase`**: Base actor for command handlers
  - Provides `HandleCmdInSession()` wrapper with session validation
  - Auto-appends `SessionActivityRecordedEvent`
  - Handles exceptions and rolls back on failure
  - Exposes `DateTimeProvider` and `GuidProvider` for testability

- **`CmdRouterBase`**: Base actor for command routers
  - Use `ForwardMessage<TCmd>(Props)` to register command handlers

- **`V1CommandControllerBase`/`V1QueryControllerBase`**: Base controllers with common setup

### Session Management & Authentication

- **Auth Middleware** (`SessionValidationMiddleware`): Validates Bearer token (SessionId GUID), writes to `HttpContext`
- **Exempt routes**: `/api/v1/cmd/session/create`, `/api/auth/token`
- **Session validation**: All commands auto-validate session is not closed via `CmdHandlerBase.HandleCmdInSession()`
- Extract SessionId in controllers: `HttpContext.GetSessionId()`

### Critical Patterns

#### Actor DI Pattern
```csharp
// ✅ CORRECT: Pass IServiceProvider, create scopes per message
public class SomeCmdHandler : CmdHandlerBase
{
    public SomeCmdHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ReceiveAsync<SomeCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private async Task<CommandResult> HandleMessage(SomeCmd cmd, IDocumentSession session, Session sessionObj)
    {
        // session is scoped to this message
        var user = await session.LoadAsync<User>(cmd.UserId);
        // ... business logic
        session.Events.Append(userId, new SomeEvent(...));
        return new CommandResult(cmd, true, resultData);
    }

    public static Props Prop(IServiceProvider sp) =>
        Props.Create(() => new SomeCmdHandler(sp));
}
```

#### Controller Pattern
```csharp
// ✅ Commands: Inject IRequiredActor, use Ask pattern
public class UserController : V1CommandControllerBase
{
    private readonly IActorRef _router;

    public UserController(IRequiredActor<UserManagementCmdRouter> router)
    {
        _router = router.ActorRef;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _router.Ask<CommandResult>(
            new CreateUserCmd(sessionId, req.UserName, req.Email));

        return result.Success
            ? Ok(new { UserId = (Guid)result.ResultData })
            : StatusCode(500, new { result.ErrorMessage });
    }
}

// ✅ Queries: Inject IDocumentStore, create lightweight sessions
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

#### Projection Pattern
```csharp
public class UserProjection : SingleStreamProjection<User, Guid>
{
    // Create from first event
    public User Create(UserCreatedEvent evt) =>
        new(evt.UserId, evt.UserName, evt.Email, null, false, evt.CreatedAt, evt.CreatedAt);

    // Apply subsequent events (use record 'with' syntax)
    public User Apply(User user, PasswordAuthenticationAddedEvent evt) =>
        user with { Password = evt.PasswordHash };

    public User Apply(User user, UserDeactivatedEvent _) =>
        user with { IsDeactivated = true };
}
```

### Actor Registration (Program.cs)
```csharp
builder.Services.AddAkka("akka-universe", (builder, sp) =>
{
    builder.WithActors((system, registry) =>
    {
        var router = system.ActorOf(UserManagementCmdRouter.Prop(sp), "UserManagementCmdRouter");
        registry.Register<UserManagementCmdRouter>(router);
    });
});
```

## Adding New Commands

### Manual Process (see MartenAkkaTests.Api/docs/Scaffolding.md for planned automation)

1. **Create Command File** in `Domain/Cmd/CommandName.cs`:
   - Define `CommandNameCmd` record (implements `ICmd`)
   - Define `CommandNameEvent` record(s) (implement `IDomainEvent`)
   - Define `CommandNameCmdHandler` class (extends `CmdHandlerBase`)

2. **Register in Router**: Add `ForwardMessage<CommandNameCmd>(CommandNameCmdHandler.Prop(serviceProvider))` to `DomainManagementCmdRouter` constructor

3. **Update Projection**: Add `Apply()` method in `DomainProjection` for new events

4. **Add Controller Endpoint**: Create endpoint in `api/v1/cmd/DomainController.cs`

5. **Define Request DTO**: Create `CommandNameRequest` record in `Controller/v1/cmd/Requests/`

## Testing

- **Framework**: NUnit with Akka.TestKit
- **Mocking**: NSubstitute for IDateTimeProvider/IGuidProvider
- **Base Class**: `ActorTestBase` provides test actor system setup
- **Pattern**: Test handlers in isolation by mocking document session

## Common Pitfalls

- ❌ **Never inject `IDocumentSession` into actors** → Session scope conflicts across messages
- ❌ **Don't use inline projections for rebuilds** → Use `RebuildProjectionActor` instead
- ❌ **Don't mutate state in query controllers** → Strictly read-only
- ❌ **Don't skip session validation** → All commands must have valid SessionId (except session creation)

## Conventions

- **Naming**: `{Verb}{Entity}Cmd` (e.g., `CreateUserCmd`), `{Entity}{PastTense}Event` (e.g., `UserCreatedEvent`)
- **File per Command**: Each command gets its own file containing Cmd + Event(s) + Handler
- **Records everywhere**: Use C# records for immutable DTOs (Commands, Events, Aggregates)
- **API Versioning**: Group by version + CQRS (`[ApiExplorerSettings(GroupName = "v1-commands")]`)
- **Error Handling**: Return `CommandResult` with descriptive error messages, never throw in handlers
