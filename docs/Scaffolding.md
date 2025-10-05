# Scaffolding

## Übersicht

Dieses Projekt ist eine Referenzimplementation für ein CQRS mit EventSourcing Pattern unter Verwendung von
Marten mit scoped Command Services inklusive HTTP Endpoints.

Die Architektur ist deutlich schlanker als die ursprüngliche Akka.NET-basierte Version, bringt aber immer noch
strukturierten Boilerplate mit.

Für die schnelle Iteration wollen wir eine deterministische Code Generierung mit AI enhanced Coding kombinieren.

Auf diese Weise soll die AI hoch kohärenten Code erzeugen können ohne jedes Mal das Rad neu erfinden zu müssen.

## Architektur

### REST / Controller

#### Authentifizierung / Autorisierung

Die Authentifizierung erfolgt über eine Middleware (`SessionValidationMiddleware`). Die Autorisierung ist noch nicht implementiert.

Lediglich die Routen /api/v1/cmd/session/create und /api/auth/token benötigen keine Authentifizierung.

Die Authentifizierung erfolgt über einen Auth Header (Bearer), der eine Guid (SessionId) enthält. Diese entspricht
der ID der Session (Domain Objekt). Die Auth Middleware lädt das Session-Aggregat und schreibt sowohl SessionId
als auch Session-Objekt in den HttpContext.

Ein `CmdModelBinder` injiziert die SessionId automatisch in alle Commands (ICmd), so dass Controller keine
Request-DTOs mehr benötigen.

#### Struktur

Die Controller / Endpunkte sind versioniert und in api/v1/qry und  api/v1/cmd getrennt.

##### Query

Query Controller bekommen in der Regel den Marten "IDocumentStore" injected. Auf der Query Seite sind wir ansonsten
völlig frei, was die Implementierung angeht und können später auch andere Projections und andere Backends
einbauen. Wichtig ist, dass die qry Seite den State niemals mutiert.

##### Cmd

Command Controller bekommen einen CommandService (z.B. `UserCommandService`) injected.
Der CommandService enthält Methoden für jedes Command (z.B. `CreateUser(CreateUserCmd)`).

Die CommandController antworten immer mit einem "CommandResult".

- Die Funktionalität ist in logische Bereiche getrennt (UserManagement, SessionManagement, ...)
- Jeder Bereich enthält genau einen CommandService und ein Verzeichnis "Cmd"
- In jedem Bereich kann es mehrere "Aggregates" geben (z.B. User, Session)
- Jedes Aggregate beinhaltet i.d.R. eine "Projection", die es aus den Events heraus mutiert.

##### Commands

Im Verzeichnis Cmd eines Bereiches ist für jedes Cmd eine Datei, z.B. "CreateUser.cs".

Diese Datei beinhaltet zwei Klassen/Records:

--> Das Command (record, z.B. "CreateUserCmd") - implementiert ICmd
--> Das oder die Events, die ausgelöst werden können (z.B: "UserCreatedEvent") - implementiert IDomainEvent

Die Business Logik befindet sich in einer Methode des CommandService (z.B. `UserCommandService.CreateUser()`).

## Ablauf Cmd (query ist trivial)

### 1. Controller

Cmd kommt auf Controller herein. Middleware hat bereits authentifiziert und SessionId in Command injiziert:

```C#
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserCmd cmd) // SessionId auto-injected
    {
        var result = await _userCommandService.CreateUser(cmd);

        return result.Success
            ? Ok(new { UserId = (Guid)result.ResultData })
            : StatusCode(500, new { result.ErrorMessage });
    }
```

### 2. CommandService

```C#
public class UserCommandService : CommandServiceBase
{
    public UserCommandService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<CommandResult> CreateUser(CreateUserCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var existingUser = await session.Query<User>()
                .Where(u => u.UserName == cmd.UserName || u.Email == cmd.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return existingUser.UserName == cmd.UserName
                    ? new CommandResult(cmd, false, null, "Username already exists")
                    : new CommandResult(cmd, false, null, "Email already exists");
            }

            var userId = GuidProvider.NewGuid();
            session.Events.StartStream<User>(userId,
                new UserCreatedEvent(sessionObj.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
        });
    }
}
```

### 3. Command + Event Records

```C#
// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;
```

### 4. Projection

```C#
// Aggregate
public record User(Guid UserId, string UserName, string Email, string? Password, bool IsDeactivated, DateTime CreatedAt, DateTime LastUpdatedAt);

// Projection
public class UserProjection : SingleStreamProjection<User, Guid>
{
    public User Create(UserCreatedEvent evt) =>
        new (evt.UserId, evt.UserName, evt.Email, null, false, evt.CreatedAt, evt.CreatedAt);
```

## Zusammenfassung

Für eine Funktion "Create a new user" muss man also

1. Die Records "CreateUserCmd" und "UserCreatedEvent" in `UserManagement/Cmd/CreateUser.cs` definieren
2. Eine Methode `CreateUser()` im `UserCommandService` erstellen mit Business Logik
3. Eine `Apply()` Methode für "UserCreatedEvent" in der `UserProjection` hinzufügen (ggf. Aggregate anpassen - non-breaking!)
4. Einen Endpunkt im UserController hinzufügen, der das Command annimmt und an Service weiterleitet

Das Scaffolding dafür sollte deterministisch per Script erfolgen können, wenn man den Namen des Commands und die Command
Properties kennt. Für die Apply-Methode und die Service-Methode können Platzhalter generiert werden.

## Geplanter Ablauf. Beispiel: Ändere User-Email

Prompt an LLM: "Wir wollen es dem User erlauben seine Email zu ändern".

### Phase 1: Ermittlung der Eingangsparameter für das Script
1. LLM prüft Aggregate und findet "User" und seine Properties.
2. --> AggregateName = "User"
3. --> CmdName = "ChangeUserEmail"
4. --> EventName ="UserChanged"
5. LLM entscheidet über die relevanten Properties (z.B. "string newEmail")

### Phase 2: Aufruf des Scripts

6. LLM ruft ein GeneratorScript auf, dass auf Basis von Templates die Datei
   - ChangeUserEmail.cs (mit den Records ChangeUserEmailCmd und UserEmailChangedEvent) erzeugt
7. Es wird eine Methode `ChangeUserEmail()` im `UserCommandService` erstellt (Template mit Platzhaltern)
8. Es wird in der UserProjection eine ApplyFunktion (ebenfalls aus einem Template) erstellt
9. Es wird im UserController ein neuer Endpunkt generiert (ebenfalls aus einem Template)

### Phase 3: Ersetzen der Platzhalter

10. Das LLM erzeugt die Implementierung für die Service-Methode und die Apply-Funktion (Ersetzen von Placeholdern aus den Templates)

Danach übernimmt der User wieder die Implementierung (Prüfung, Test, ...)

## Vorteile der neuen Architektur

Die Umstellung von Akka.NET auf Command Services hat folgende Vorteile:

1. **Weniger Boilerplate**: Keine Actor-Registrierung, keine Props-Factories, keine Router
2. **Einfacheres Debugging**: Standard C# Methoden statt Actor Message Passing
3. **Bessere IDE-Unterstützung**: IntelliSense funktioniert direkt, keine Reflection
4. **Klarere Dependency Injection**: Scoped Services statt IServiceProvider in Actors
5. **Reduzierte Komplexität**: ~800 LOC weniger Code bei gleicher Funktionalität
6. **Keine Actor-spezifischen Bugs**: Keine Deadlocks, keine Ask-Timeouts, keine Actor-Lifecycle-Issues