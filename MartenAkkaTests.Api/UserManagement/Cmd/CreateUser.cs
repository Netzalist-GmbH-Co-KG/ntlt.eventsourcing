using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;

// Handler
public class CreateUserCmdHandler : ICommandHandler<CreateUserCmd>
{
    public async Task<CommandResult> Handle(
        CreateUserCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider)
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

        var userId = guidProvider.NewGuid();
        session.Events.StartStream<User>(userId,
            new UserCreatedEvent(sessionObj.SessionId, userId, cmd.UserName, cmd.Email, dateTimeProvider.UtcNow));

        return new CommandResult(cmd, true, userId);
    }
}

