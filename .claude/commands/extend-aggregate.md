# Extend Aggregate

Du bist ein Experte für CQRS Event Sourcing mit Marten. Erweitere ein **existierendes Aggregate** um neue Properties oder Business Logic.

## Use Cases

- Property zu Aggregate hinzufügen (z.B. `User.PhoneNumber`)
- Neues Event für existierendes Aggregate (z.B. `UserEmailChanged`)
- Business Logic zu existierendem Command ergänzen

## Workflow

### 1. Analysiere Anforderung

$ARGUMENTS

Identifiziere:
- **Aggregate**: Welches? (User, Session, ...)
- **Neue Property**: Welche? (Optional)
- **Neues Event**: Welches? (Optional)
- **Command**: Neues oder erweitern?

### 2. Aggregate erweitern (falls neue Property)

In `{Domain}/{Aggregate}.cs`:

```csharp
// ALT
public record User(Guid UserId, string UserName, string Email, ...);

// NEU (non-breaking change via optional property)
public record User(
    Guid UserId,
    string UserName,
    string Email,
    string? PhoneNumber = null,  // Neue Property mit Default
    ...);
```

### 3. Event + Command erstellen

Erstelle `{Domain}/Cmd/{CommandName}.cs`:

```csharp
public record {CommandName}Cmd(Guid? SessionId, Guid UserId, /* neue Properties */) : ICmd;

public record {EventName}Event(
    Guid SessionId,
    Guid UserId,
    /* neue Properties */,
    DateTime Timestamp) : IDomainEvent;
```

### 4. Projection erweitern

Füge Apply-Methode hinzu:

```csharp
public class UserProjection : SingleStreamProjection<User, Guid>
{
    // ... existing methods ...

    public User Apply(User user, {EventName}Event evt) =>
        user with { PhoneNumber = evt.PhoneNumber };
}
```

### 5. CommandService Method

Füge Methode zu CommandService hinzu (wie in `/add-feature`).

### 6. Controller Endpoint

Füge Endpoint hinzu (wie in `/add-feature`).

### 7. Tests

Schreibe Tests für neue Funktionalität.

## Migration Strategy

**Wichtig**: Event Sourcing ist append-only!

✅ **Safe Changes**:
- Neue Properties mit Default-Werten (`string? PhoneNumber = null`)
- Neue Events (alte Events funktionieren weiter)
- Neue Apply-Methoden (alte Projections nicht betroffen)

❌ **Breaking Changes vermeiden**:
- Properties umbenennen → Erstelle neue Property + deprecate alte
- Properties entfernen → Deprecate + ignorieren in Projection
- Event-Schema ändern → Erstelle neue Event-Version

## Beispiel: User Phone Number

```csharp
// 1. Aggregate erweitern
public record User(
    Guid UserId,
    string UserName,
    string Email,
    string? Password,
    bool IsDeactivated,
    string? PhoneNumber = null,  // NEU
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

// 2. Command + Event
public record SetPhoneNumberCmd(Guid? SessionId, Guid UserId, string PhoneNumber) : ICmd;
public record PhoneNumberSetEvent(Guid SessionId, Guid UserId, string PhoneNumber, DateTime Timestamp) : IDomainEvent;

// 3. Projection Apply
public User Apply(User user, PhoneNumberSetEvent evt) =>
    user with { PhoneNumber = evt.PhoneNumber, LastUpdatedAt = evt.Timestamp };

// 4. CommandService
public async Task<CommandResult> SetPhoneNumber(SetPhoneNumberCmd cmd)
{
    return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
    {
        var user = await session.LoadAsync<User>(cmd.UserId);
        if (user == null)
            return new CommandResult(cmd, false, null, "User not found");

        session.Events.Append(cmd.UserId,
            new PhoneNumberSetEvent(sessionObj.SessionId, cmd.UserId, cmd.PhoneNumber, DateTimeProvider.UtcNow));

        return new CommandResult(cmd, true);
    });
}
```

## Abschluss

Zeige:
1. **Zusammenfassung** aller Änderungen
2. **Migration Notes** (falls Breaking Changes)
3. **Build & Test Ergebnis**

Kein Commit - User reviewt zuerst!

---

**Bereit!** Was soll erweitert werden?
