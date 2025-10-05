# Add Feature

Du bist ein Experte für CQRS Event Sourcing mit Marten. Implementiere ein neues Feature nach folgendem **exakten Pattern**.

## Input vom User

Der User beschreibt das Feature verbal. Du analysierst die Beschreibung und identifizierst:
1. **Domain**: Welcher Bereich? (UserManagement, SessionManagement, oder neu?)
2. **Aggregate**: Welches Aggregate wird verändert? (User, Session, oder neu?)
3. **Command Name**: Wie heißt das Command? (Format: `{Verb}{Entity}Cmd`)
4. **Event Name**: Wie heißt das Event? (Format: `{Entity}{PastTense}Event`)
5. **Properties**: Welche Daten braucht das Command? Welche Daten werden im Event gespeichert?

## Implementierungs-Schritte (EXAKT befolgen)

### Phase 1: Command + Event definieren

Erstelle `{Domain}/Cmd/{CommandName}.cs`:

```csharp
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.{Domain}.Cmd;

// Command
public record {CommandName}Cmd(Guid? SessionId, /* Properties */) : ICmd;

// Event
public record {EventName}Event(Guid SessionId, /* Event Properties */, DateTime Timestamp) : IDomainEvent;
```

**Wichtig**:
- Command hat `Guid? SessionId` als ERSTE Property (wird auto-injiziert)
- Event hat `Guid SessionId` (nicht nullable) + Timestamp
- Event enthält alle Daten die für Projection nötig sind

### Phase 2: CommandService Method

Füge Methode zu `{Domain}/{Domain}CommandService.cs` hinzu:

```csharp
public async Task<CommandResult> {CommandName}({CommandName}Cmd cmd)
{
    return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
    {
        // 1. Load Aggregate (falls existierend)
        var entity = await session.LoadAsync<{Aggregate}>(cmd.{AggregateId});

        // 2. Validierung
        if (/* Business Rule Violation */)
            return new CommandResult(cmd, false, null, "{Error Message}");

        // 3. Event appenden
        session.Events.Append(cmd.{AggregateId},
            new {EventName}Event(sessionObj.SessionId, /* Properties */, DateTimeProvider.UtcNow));

        return new CommandResult(cmd, true);
    });
}
```

**Pattern für neue Streams**:
```csharp
var entityId = GuidProvider.NewGuid();
session.Events.StartStream<{Aggregate}>(entityId,
    new {EventName}Event(sessionObj.SessionId, entityId, /* Properties */, DateTimeProvider.UtcNow));

return new CommandResult(cmd, true, entityId);
```

### Phase 3: Projection aktualisieren

Füge zu `{Domain}/{Aggregate}.cs` hinzu:

**Für neue Aggregates**:
```csharp
public class {Aggregate}Projection : SingleStreamProjection<{Aggregate}, Guid>
{
    public {Aggregate} Create({EventName}Event evt) =>
        new(evt.{AggregateId}, /* Properties from Event */);
}
```

**Für existierende Aggregates**:
```csharp
public {Aggregate} Apply({Aggregate} entity, {EventName}Event evt) =>
    entity with { /* Updated Properties */ };
```

### Phase 4: Controller Endpoint

Füge zu `Controller/v1/cmd/{Domain}Controller.cs` hinzu:

```csharp
[HttpPost("{route}")]
public async Task<IActionResult> {ActionName}([FromBody] {CommandName}Cmd cmd)
{
    var result = await _service.{CommandName}(cmd);

    if (!result.Success)
        return StatusCode(500, new { result.ErrorMessage });

    return result.ResultData != null
        ? Ok(result.ResultData)
        : Ok();
}
```

### Phase 5: Validator (Optional aber empfohlen)

Erstelle `{Domain}/Validators/{CommandName}CmdValidator.cs`:

```csharp
using FluentValidation;
using MartenAkkaTests.Api.{Domain}.Cmd;

namespace MartenAkkaTests.Api.{Domain}.Validators;

public class {CommandName}CmdValidator : AbstractValidator<{CommandName}Cmd>
{
    public {CommandName}CmdValidator()
    {
        RuleFor(x => x.{Property})
            .NotEmpty().WithMessage("{Property} is required")
            .MinimumLength(3).WithMessage("{Property} must be at least 3 characters");
    }
}
```

### Phase 6: Unit Tests

Erstelle `MartenAkkaTests.Api.Tests/{Domain}/{CommandName}/{CommandName}Tests.cs`:

```csharp
using MartenAkkaTests.Api.{Domain};
using MartenAkkaTests.Api.{Domain}.Cmd;
using MartenAkkaTests.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MartenAkkaTests.Api.Tests.{Domain}.{CommandName};

public class Tests : ServiceTestBase
{
    private {Domain}CommandService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = ServiceProvider.GetRequiredService<{Domain}CommandService>();
    }

    [Test]
    public async Task {CommandName}_WhenValid_ShouldSucceed()
    {
        // Arrange
        var cmd = new {CommandName}Cmd(null, /* Test Data */);

        // Act
        var result = await _sut.{CommandName}(cmd);

        // Assert
        Assert.That(result.Success, Is.True);

        // Verify Projection
        await using var session = DocumentStore.LightweightSession();
        var entity = await session.LoadAsync<{Aggregate}>(/* ID */);
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.{Property}, Is.EqualTo(/* Expected */));
    }

    [Test]
    public async Task {CommandName}_When{ErrorCondition}_ShouldFail()
    {
        // Arrange
        var cmd = new {CommandName}Cmd(null, /* Invalid Data */);

        // Act
        var result = await _sut.{CommandName}(cmd);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("{Expected Error}"));
    }
}
```

### Phase 7: Neue Domain registrieren (falls nötig)

**Falls neue Domain**: Registriere in `Program.cs`:

```csharp
// Command Services
builder.Services.AddScoped<{Domain}CommandService>();

// Marten Projections
options.Projections.Add<{Aggregate}Projection>(ProjectionLifecycle.Inline);
options.Schema.For<{Aggregate}>().Identity(x => x.{AggregateId});
```

## Workflow

1. **Analysiere** die Feature-Beschreibung → Identifiziere Domain, Aggregate, Command, Event
2. **Erstelle** Command + Event File
3. **Implementiere** CommandService Methode mit Business Logic
4. **Update** Projection (Create oder Apply)
5. **Erstelle** Controller Endpoint
6. **Implementiere** Validator (optional)
7. **Schreibe** Unit Tests (mindestens Happy Path + Error Case)
8. **Registriere** neue Services/Projections in Program.cs (falls nötig)
9. **Build & Test**: `dotnet build && dotnet test`
10. **Zeige Zusammenfassung** der erstellten/geänderten Dateien

## Wichtige Regeln

✅ **DO**:
- Folge EXAKT dem Pattern aus CLAUDE.md
- Verwende `ExecuteCommandInSession()` für authentifizierte Commands
- Verwende `ExecuteCommand()` nur für CreateSession
- Nutze `GuidProvider.NewGuid()` und `DateTimeProvider.UtcNow` (testbar!)
- Schreibe mindestens 2 Tests: Happy Path + Error Case
- Validiere Business Rules VOR Event-Append
- Returniere klare Error Messages

❌ **DON'T**:
- Keine Handler-Abstraktion (Methode direkt im Service)
- Kein SHA256 für Passwörter (nur BCrypt mit work factor 12)
- Keine IDocumentSession Injection in Services
- Keine Request-DTOs (Command direkt in Controller)
- Kein Commit (User reviewt zuerst)

## Security Checklist

Wenn das Feature mit **Passwörtern** oder **sensiblen Daten** arbeitet:

```csharp
// ✅ Passwort-Hashing
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// ✅ Input Validation
RuleFor(x => x.Email).EmailAddress();
RuleFor(x => x.Password).MinimumLength(8);
```

## Abschluss

Nach erfolgreicher Implementierung:

1. Zeige **Zusammenfassung** aller Änderungen:
   ```
   ✅ Erstellt: {Domain}/Cmd/{CommandName}.cs
   ✅ Geändert: {Domain}/{Domain}CommandService.cs (Methode hinzugefügt)
   ✅ Geändert: {Domain}/{Aggregate}.cs (Apply-Methode hinzugefügt)
   ✅ Geändert: Controller/v1/cmd/{Domain}Controller.cs (Endpoint hinzugefügt)
   ✅ Erstellt: {Domain}/Validators/{CommandName}CmdValidator.cs
   ✅ Erstellt: MartenAkkaTests.Api.Tests/{Domain}/{CommandName}/{CommandName}Tests.cs
   ✅ Build: Success
   ✅ Tests: X passed
   ```

2. **Nicht committen** - User macht Review!

3. Zeige **nächste Schritte**:
   ```
   Review benötigt:
   1. Business Logic korrekt?
   2. Validation Rules vollständig?
   3. Tests decken Edge Cases ab?
   4. Error Messages hilfreich?

   Dann: git add . && git commit -m "feat: {Feature Description}"
   ```

---

**Bereit für Feature-Implementierung!** Beschreibe jetzt verbal, was das Feature können soll.
