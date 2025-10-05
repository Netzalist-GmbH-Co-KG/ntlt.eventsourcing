# Scaffolding

## Übersicht

Dieses Projekt ist eine Referenzimplementation für ein CQRS mit EventSourcing Pattern unter Verwendung von 
Marten in einem Akka.NET-basierten System inklusive HTTP Endpoints.

Die Architektur bringt zwangsläufig relativ viel Boilerplate mit.

Für die schnelle Iteration wollen wir eine deterministische Code Generierung mit AI enhanced Coding kombinieren.

Auf diese Weise soll die AI hoch kohärenten Code erzeugen können ohne jedes Mal das Rad neu erfinden zu müssen.

## Architektur

### REST / Controller

#### Authentifizierung / Autorisierung

Die Authentifizierung erfolgt über eine Middleware. Die Autorisierung ist noch nicht implementiert.

Lediglich die Routen /api/v1/cmd/session/create und /api/auth/token benötigen keine Authentifizierung.

Die Authentifizierung erfolgt über einen Auth Header (Bearer), der eine Guid (SessionId) enthält. Diese entspricht
der ID der Session (Domain Objekt). Die Auth Middleware schreibt die SessionId in den HttpContext.

#### Struktur

Die Controller / Endpunkte sind versioniert und in api/v1/qry und  api/v1/cmd getrennt.

##### Query

Query Controller bekommen in der Regel den Marten "IDocumentStore" injected. Auf der Query Seite sind wir ansonsten
völlig frei, was die Implementierung angeht und können später auch andere Projections und andere Backends
einbauen. Wichtig ist, dass die qry Seite den State niemals mutiert.

##### Cmd

Command Controller bekommen einen Actor (CmdRouter) injected, der als Schnittstelle zum ActorSystem fungiert.
Der CommandRouter generiert ChildActors, die die einzelnen CommandHandler beinhalten.

Die CommandController antworten immer mit einem "CommandResult".

- Die Funktionalität ist in logische Bereiche getrennt (UserManagement, SessionManagement, ...)
- Jeder Bereich enthält genau einen CommandRouter und ein Verzeichnis "Cmd"
- In jedem Bereich kann es mehrere "Aggregates" geben (z.B. User, Session)
- Jedes Aggregate beinhaltet i.d.R. eine "Projection", die es aus den Events heraus mutiert.

##### Commands

Im Verzeichnis Cmd eines Bereiches ist für jedes Cmd eine Datei, z.B. "CreateUser".

Diese Datei beinhaltet immer mehrere Klassen:

--> Das Command (POCO, record, z.B. "CreateUserCmd")
--> Das oder die Events, die ausgelöst werden können (z.B: UserCreatedEvent")
--> Den CommandHandler, der die Business Logik enthält und über Events den State mutiert.

## Ablauf Cmd (query ist trivial)

### 1. Controller

Cmd kommt auf Controller herein. Middleware hat bereits authentifiziert:

```C#
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _userManagementCmdRouter.Ask<CommandResult>(
            new CreateUserCmd(sessionId, request.UserName, request.Email));

        if (result.Success && result.ResultData != null)
        {
            return Ok(new { UserId = (Guid)result.ResultData });
        }

        return StatusCode(500, new { result.ErrorMessage });
    }
```

### 2. CommandRouter

```C#
public class UserManagementCmdRouter : CmdRouterBase
{
    public UserManagementCmdRouter(IServiceProvider serviceProvider)
    {
        ForwardMessage<CreateUserCmd>(CreateUserCmdHandler.Prop(serviceProvider));
```

### 3. CommandHandler

```C#
// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;

// Cmd Handler
public class CreateUserCmdHandler : CmdHandlerBase
{
    public CreateUserCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<CreateUserCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private async Task<CommandResult> HandleMessage(CreateUserCmd cmd, IDocumentSession documentSession, Session session)
    {
            var existingUser = await documentSession.Query<User>()
                .Where(u => u.UserName == cmd.UserName || u.Email == cmd.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return existingUser.UserName == cmd.UserName 
                    ? new CommandResult(cmd, false, null,"Username already exists") 
                    : new CommandResult(cmd, false, null, "Email already exists");
            }

            var userId = GuidProvider.NewGuid();
            documentSession.Events.StartStream<User>(userId,
                new UserCreatedEvent(session.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new CreateUserCmdHandler(serviceProvider));
}
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

1. Die Records "CreateUserRequest", "CreateUserCmd" und "UserCreatedEvent" definieren.
2. Einen CommandHandler erstellen, der das Command handelt und ein "UserCreatedEvent" persistiert
3. Den CommandHandler im UserManagementCmdRouter registrieren.
4. Einen Reducer für "UserCreatedEvent" im Aggregate "User" hinzufügen. GGf. Aggregate anpassen (non-breaking!)
5. Einen Endpunkt im UserController hinzufügen, der das Command annimmt und weiterleitet.


Das Scaffolding dafür sollte deterministisch per Script erfolgen können, wenn man den Namen des Commands und die Command 
Properties kennt. Für den Reducer und den Command Handler können Platzhalter generiert werden.

## Geplanter Ablauf. Beispiel: Ändere User-Email

Prompt an LLM: "Wir wollen es dem User erlauben seine Email zu ändern".

### Phase 1: Ermittlung der Eingangsparameter für das Script
1. LLM prüft Aggregate und findet "User" und seine Properties.
2. --> AggregateName = "User"
3. --> CmdName = "ChangeUserEmail"
4. --> EventName ="UserChanged"
5. LLM entscheidet über die relevanten Properties (z.B. "string newEmail")

### Phase 2: Aufruf des Scripts

6. LLM ruft ein GeneratorScript auf, dass auf Basis von Templates die Dateien
   - ChangeUserEmailRequest
   - ChangeUserEmail (mit den Klassen / Records ChangeUserEmailCmd, ChangeUserEmailCmdHandler und UserEmailChangedEvent)
   erzeugt
7. Außerdem wird der neue Actor im UserManagementRouter registriert
8. Es wird in der UserProjection eine ApplyFunktion (ebenfalls aus einem Template) erstellt
9. Es wird im UserController ein neuer Endpunkt generiert (ebenfalls aus einem Template)

### Phase 3: Ersetzen der Platzhalter

10. Das LLM erzeugt die Implementierung für den CommandHandler und den Reducer (Ersetzen von Placeholdern aus den Templates)


Danach übernimmt der User wieder die Implementierung (Prüfung, Test, ...)