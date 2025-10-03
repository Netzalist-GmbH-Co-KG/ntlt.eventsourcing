# Architecture Decisions: Event Sourcing + Akka.NET

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
- **Actor System**: Akka.NET 1.5.51 + Akka.Hosting
- **Framework**: ASP.NET Core 9.0
- **Projections**: Inline (strong consistency) and Async (eventual consistency)

## Architectural Patterns

### 1. Actor-Based Aggregates

```csharp
public class SomethingActor : ReceiveActor
{
    private readonly IServiceProvider _serviceProvider;

    public SomethingActor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ReceiveAsync<SomethingHappenedCommand>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(SomethingHappenedCommand cmd)
    {
        // Create scoped session for each message
        using var scope = _serviceProvider.CreateScope();
        var session = scope.ServiceProvider.GetService<IDocumentSession>();

        session.Events.Append(cmd.Id, new SomethingHappened(cmd.Id, DateTime.Now.ToShortTimeString()));
        await session.SaveChangesAsync();

        // Reply with result
        Sender.Tell(new HandledOkNotification(cmd.Id, newCounter));
    }

    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new SomethingActor(serviceProvider));
}
```

**Key Learnings:**
- ✅ Pass `IServiceProvider` to actors, create scopes per message
- ✅ Use `Ask` pattern with typed responses (not fire-and-forget `Tell`)
- ✅ Register actors via `Akka.Hosting` for clean DI integration

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
public class TestController : ControllerBase
{
    private readonly IActorRef _somethingActor;
    private readonly IDocumentStore _store; // NOT IDocumentSession!

    public TestController(
        IRequiredActor<SomethingActor> somethingActor,
        IDocumentStore store)
    {
        _somethingActor = somethingActor.ActorRef;
        _store = store;
    }

    [HttpGet("/test")]
    public async Task<IActionResult> Get()
    {
        var reply = await _somethingActor.Ask<HandledOkNotification>(
            new SomethingActor.SomethingHappenedCommand(id),
            TimeSpan.FromSeconds(30)
        );
        return Ok($"Accepted: {reply.NewCounter}");
    }

    [HttpGet("/query")]
    public async Task<IActionResult> Query()
    {
        // Create session manually for queries
        await using var session = _store.LightweightSession();
        var counter = await session.LoadAsync<SomethingCounter>(id);
        return Ok(counter);
    }
}
```

**Key Learnings:**
- ✅ Inject `IRequiredActor<T>` for type-safe actor access
- ✅ Inject `IDocumentStore` (not `IDocumentSession`) in controllers
- ✅ Create lightweight sessions per query
- ✅ Use `Ask` with timeout for proper error handling

### 4. DI Registration (Program.cs)

```csharp
// Marten
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.Projections.Add<SomethingCounterProjection>(ProjectionLifecycle.Inline);
})
.UseLightweightSessions();

// Akka.Hosting
builder.Services.AddAkka("akka-universe", (builder, sp) =>
{
    builder.WithActors((system, registry) =>
    {
        var actor = system.ActorOf(SomethingActor.Prop(sp), "somethingActor");
        registry.Register<SomethingActor>(actor);
    });
});
```

**Key Learnings:**
- ✅ Use `Akka.Hosting` instead of manual `ActorSystem` lifecycle
- ✅ Register projections at startup
- ✅ Pass `IServiceProvider` to actor `Props` for DI access

## Operational Patterns

### Projection Rebuild

```csharp
// RebuildProjectionActor allows manual projection rebuild
[HttpGet("/rebuild")]
public async Task<IActionResult> RebuildProjections([FromQuery] string? projection = null)
{
    var result = await _rebuildActor.Ask<object>(
        new RebuildProjectionActor.RebuildProjectionCommand(projection),
        TimeSpan.FromMinutes(5)
    );
    // Returns stats of rebuilt projections
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
- **Inline projections don't rebuild**: Delete projection docs manually or use rebuild actor

### ❌ Actor Issues
- **Injecting IDocumentSession**: Shared state across messages → use `IServiceProvider` + scopes
- **Null ActorRef**: Register actors before use

### ❌ DI Issues
- **Session scope mismatch**: Controller gets scoped session, actor creates own → inconsistent reads

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

## Next Steps: Template Creation

### TODO List

1. **Cleanup Prototype**
   - [ ] Consistent namespaces and XML docs
   - [ ] Add integration tests

2. **Documentation**
   - [ ] Onboarding guide: "ES for New Devs"
   - [ ] Pattern library: Actor + Projection + Controller
   - [ ] AI prompt templates for Claude

3. **Scaffolding**
   - [ ] `dotnet new` templates for aggregates/projections
   - [ ] Base classes: `BaseAggregateActor<TCommand, TResponse>`
   - [ ] Script: `new-aggregate.sh <EntityName>`

4. **Tooling Investment**
   - [ ] Event browser UI (Marten dashboard or custom)
   - [ ] Projection lag monitoring (Grafana)
   - [ ] Replay scripts for production fixes
   - [ ] Schema registry for event versioning

5. **Operations**
   - [ ] Docker Compose: Postgres + Seq (logging) + Grafana
   - [ ] CI/CD pipeline for actor deployments
   - [ ] Runbooks: "Projection behind", "Rebuild failed"

### Recommended Project Structure

```
YourCompany.Template/
├── Domain/
│   ├── Aggregates/          # Event-sourced entities
│   ├── Events/              # Event definitions
│   └── Commands/            # Command messages
├── Actors/
│   ├── AggregateActors/     # Business logic actors
│   └── InfrastructureActors/ # Rebuild, Saga, etc.
├── Projections/             # Read models
├── Controllers/             # API endpoints
└── Infrastructure/
    ├── ActorRegistry.cs     # Centralized actor registration
    └── MartenConfiguration.cs
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
