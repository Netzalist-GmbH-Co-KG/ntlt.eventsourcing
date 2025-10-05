using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record CreateSessionCmd(Guid? SessionId = null) : ICmd;

// Events
public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;

// Cmd Handler
public class CreateSessionCmdHandler : CmdHandlerBase
{
    public CreateSessionCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<CreateSessionCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private Task<CommandResult> HandleMessage(CreateSessionCmd cmd, IDocumentSession session)
    {
        var sessionId = GuidProvider.NewGuid();
        session.Events.StartStream<Session>(sessionId,
            new SessionCreatedEvent(sessionId, DateTimeProvider.UtcNow));
        return Task.FromResult(new CommandResult(cmd, true, sessionId));
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new CreateSessionCmdHandler(serviceProvider));
}