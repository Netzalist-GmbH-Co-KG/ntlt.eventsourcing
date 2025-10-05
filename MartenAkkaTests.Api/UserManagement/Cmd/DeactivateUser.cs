using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId) : IDomainEvent;

// Handler
public class DeactivateUserCmdHandler : ICommandHandler<DeactivateUserCmd>
{
    public async Task<CommandResult> Handle(
        DeactivateUserCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider)
    {
        var user = await session.LoadAsync<User>(cmd.UserId);
        if (user == null)
            return new CommandResult(cmd, false, null, "User not found");

        if (user.IsDeactivated)
            return new CommandResult(cmd, false, null, "User already deactivated");

        session.Events.Append(cmd.UserId, new UserDeactivatedEvent(sessionObj.SessionId, cmd.UserId));

        return new CommandResult(cmd, true);
    }
}

