using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId);

// Cmd Handler
public class DeactivateUserCmdHandler : CmdHandlerBase
{
    public DeactivateUserCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<DeactivateUserCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private async Task<CommandResult> HandleMessage(DeactivateUserCmd cmd, IDocumentSession documentSession, Session session)
    {
        var existingUser = await documentSession.Query<User>()
            .Where(u => u.UserId == cmd.UserId)
            .FirstOrDefaultAsync();

        if (existingUser == null)
        {
            return new CommandResult(cmd, false, null, "User not found");
        }

        if (existingUser.IsDeactivated)
        {
            // Idempotent - already deactivated
            return new CommandResult(cmd,true);
        }

        documentSession.Events.Append(cmd.UserId,
            new UserDeactivatedEvent(session.SessionId, cmd.UserId));
        documentSession.Events.Append(session.SessionId,
            new SessionActivityRecordedEvent(session.SessionId, DateTimeProvider.UtcNow));
        return new CommandResult(cmd,true);
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new DeactivateUserCmdHandler(serviceProvider));
}
