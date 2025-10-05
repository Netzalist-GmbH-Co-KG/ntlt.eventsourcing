using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record EndSessionCmd(Guid? SessionId, string Reason) : ICmd;

// Event
public record SessionEndedEvent(Guid SessionId, string Reason, DateTime EndedAt);

// Cmd Handler
public class EndSessionCmdHandler : CmdHandlerBase
{
    public EndSessionCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<EndSessionCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private Task<CommandResult> HandleMessage(EndSessionCmd cmd, IDocumentSession documentSession, Session session)
    {
        documentSession.Events.Append(session.SessionId,
            new SessionEndedEvent(session.SessionId, cmd.Reason, DateTimeProvider.UtcNow));

        return  Task.FromResult(new CommandResult(cmd, true));
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new EndSessionCmdHandler(serviceProvider));
}