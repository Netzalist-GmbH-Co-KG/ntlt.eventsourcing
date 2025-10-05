# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CQRS Event Sourcing system using Marten (PostgreSQL-based event store) with scoped command services for business logic. ASP.NET Core 9.0 API with strict command/query separation.

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

**Controller → CommandService → Events → Projection**

1. **Controller** (`api/v1/cmd/*`): Receives HTTP request, SessionId auto-injected via ModelBinder, calls CommandService method
2. **CommandService**: Validates business rules, appends events to Marten stream, returns `CommandResult`
3. **Projection**: Marten inline projection rebuilds aggregate state from events

### Query Flow

**Controller → DocumentStore → Projection (read model)**

Query controllers inject `IDocumentStore` and create lightweight sessions to query projections. Never mutate state on query side.

### File Organization by Domain

Each business domain (UserManagement, SessionManagement) follows this structure:

```
DomainName/
├── DomainAggregate.cs          # Record + Projection (e.g., User, Session)
├── DomainCommandService.cs     # Command service with business logic methods
└── Cmd/
    └── CommandName.cs          # Contains 2 classes:
        ├── CommandNameCmd      # Command record (implements ICmd)
        └── CommandNameEvent    # Event record (implements IDomainEvent)
```

**Example**: `UserManagement/Cmd/CreateUser.cs` contains `CreateUserCmd` and `UserCreatedEvent`. Business logic is in `UserCommandService.CreateUser()` method.

### Key Base Classes

- **`CommandServiceBase`**: Base class for command services
  - Provides `ExecuteCommand()` for commands without session validation
  - Provides `ExecuteCommandInSession()` with automatic session validation and HttpContext optimization
  - Session loaded from `HttpContext` (set by middleware) to avoid redundant DB queries
  - Falls back to DB query in test scenarios
  - Structured logging with Serilog for observability
  - Handles exceptions and transaction rollback with proper error messages
  - Exposes `DateTimeProvider` and `GuidProvider` for testability

- **`V1CommandControllerBase`/`V1QueryControllerBase`**: Base controllers with common setup

### Session Management & Authentication

- **Auth Middleware** (`SessionValidationMiddleware`): Validates Bearer token (SessionId GUID), loads Session aggregate, writes both to `HttpContext`
- **Exempt routes**: `/api/v1/cmd/session/create`, `/api/auth/token`
- **SessionId Auto-Injection**: `CmdModelBinder` automatically injects SessionId from `HttpContext` into command `SessionId` property
- **Session validation**: Commands requiring session use `ExecuteCommandInSession()` which validates session is not closed

### Critical Patterns

#### Command Service Pattern
```csharp
public class UserCommandService : CommandServiceBase
{
    public UserCommandService(IServiceProvider serviceProvider, ILogger<UserCommandService> logger)
        : base(serviceProvider, logger)
    {
    }

    public async Task<CommandResult> CreateUser(CreateUserCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            // Validate business rules
            var existingUser = await session.Query<User>()
                .Where(u => u.UserName == cmd.UserName)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return new CommandResult(cmd, false, null, "Username already exists");

            // Append event
            var userId = GuidProvider.NewGuid();
            session.Events.StartStream<User>(userId,
                new UserCreatedEvent(sessionObj.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
        });
    }

    public async Task<CommandResult> AddPasswordAuthentication(AddPasswordAuthenticationCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var user = await session.LoadAsync<User>(cmd.UserId);
            if (user == null)
                return new CommandResult(cmd, false, null, "User not found");

            if (user.Password != null)
                return new CommandResult(cmd, false, null, "Password already set");

            // Use BCrypt for secure password hashing
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password, workFactor: 12);

            session.Events.Append(cmd.UserId,
                new PasswordAuthenticationAddedEvent(sessionObj.SessionId, cmd.UserId, passwordHash));

            return new CommandResult(cmd, true);
        });
    }
}
```

#### Controller Pattern
```csharp
// ✅ Commands: Inject CommandService, use ModelBinding for auto SessionId injection
public class UserController : V1CommandControllerBase
{
    private readonly UserCommandService _service;

    public UserController(UserCommandService service)
    {
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserCmd cmd) // SessionId auto-injected by CmdModelBinder
    {
        var result = await _service.CreateUser(cmd);

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

#### Validation Pattern (Optional but Recommended)
```csharp
public class CreateUserCmdValidator : AbstractValidator<CreateUserCmd>
{
    public CreateUserCmdValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
```

### Service Registration (Program.cs)
```csharp
// Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Register command services
builder.Services.AddScoped<UserCommandService>();
builder.Services.AddScoped<SessionCommandService>();
builder.Services.AddScoped<RebuildProjectionService>();

// Register HttpContextAccessor for session optimization
builder.Services.AddHttpContextAccessor();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register ModelBinder for auto SessionId injection
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new CmdModelBinderProvider());
});
```

## Adding New Commands

### Manual Process (see docs/Scaffolding.md for planned automation)

1. **Create Command File** in `Domain/Cmd/CommandName.cs`:
   - Define `CommandNameCmd` record (implements `ICmd`)
   - Define `CommandNameEvent` record(s) (implement `IDomainEvent`)

2. **Add Service Method**: Add `public async Task<CommandResult> CommandName(CommandNameCmd cmd)` method to `DomainCommandService`

3. **Update Projection**: Add `Apply()` method in `DomainProjection` for new events

4. **Add Controller Endpoint**: Create endpoint in `api/v1/cmd/DomainController.cs` that accepts `CommandNameCmd` directly (SessionId auto-injected)

## Security

- **Password Hashing**: BCrypt with work factor 12 (NEVER use plain SHA256)
- **Input Validation**: FluentValidation for command validation (see `CreateUserCmdValidator` example)
- **Session Security**: Bearer token authentication via middleware
- **Error Handling**: Generic error messages to users, detailed logging for debugging

## Logging & Observability

- **Structured Logging**: Serilog with JSON output
- **Request Logging**: Automatic HTTP request/response logging
- **Command Logging**: All commands logged with execution time and result
- **Error Tracking**: Exceptions logged with full context (command name, session ID, stack trace)

## Testing

- **Framework**: NUnit
- **Mocking**: NSubstitute for IDateTimeProvider/IGuidProvider
- **Base Class**: `ServiceTestBase` provides Marten test setup with isolated schemas
- **Pattern**: Test command services with real Marten in-memory database

## Common Pitfalls

- ❌ **Don't inject `IDocumentSession` into services** → Use `IServiceProvider` in `CommandServiceBase` for scoped sessions
- ❌ **Don't use inline projections for rebuilds** → Use `RebuildProjectionService` instead
- ❌ **Don't mutate state in query controllers** → Strictly read-only
- ❌ **Don't skip session validation** → Use `ExecuteCommandInSession()` for commands requiring authentication

## Conventions

- **Naming**: `{Verb}{Entity}Cmd` (e.g., `CreateUserCmd`), `{Entity}{PastTense}Event` (e.g., `UserCreatedEvent`)
- **File per Command**: Each command gets its own file containing Cmd + Event(s) records only
- **Records everywhere**: Use C# records for immutable DTOs (Commands, Events, Aggregates)
- **API Versioning**: Group by version + CQRS (`[ApiExplorerSettings(GroupName = "v1-commands")]`)
- **Error Handling**: Return `CommandResult` with descriptive error messages, never throw in service methods
- **No Request DTOs**: Controllers accept `ICmd` records directly with auto-injected SessionId
