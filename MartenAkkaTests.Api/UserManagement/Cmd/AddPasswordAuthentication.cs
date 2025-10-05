using System.Security.Cryptography;
using System.Text;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record AddPasswordAuthenticationCmd(Guid? SessionId, Guid UserId, string Password) : ICmd;

// Events
public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash) : IDomainEvent;

// Handler
public class AddPasswordAuthenticationCmdHandler : ICommandHandler<AddPasswordAuthenticationCmd>
{
    public async Task<CommandResult> Handle(
        AddPasswordAuthenticationCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider)
    {
        var user = await session.LoadAsync<User>(cmd.UserId);
        if (user == null)
            return new CommandResult(cmd, false, null, "User not found");

        if (user.Password != null)
            return new CommandResult(cmd, false, null, "Password already set");

        var passwordHash = HashPassword(cmd.Password);
        session.Events.Append(cmd.UserId,
            new PasswordAuthenticationAddedEvent(sessionObj.SessionId, cmd.UserId, passwordHash));

        return new CommandResult(cmd, true);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

