using System.Security.Cryptography;
using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record AddPasswordAuthenticationCmd(Guid? SessionId, Guid UserId, string Password) : ICmd;

// Events
public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash);

// Cmd Handler
public class AddPasswordAuthenticationCmdHandler : CmdHandlerBase
{
    public AddPasswordAuthenticationCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<AddPasswordAuthenticationCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private async Task<CommandResult> HandleMessage(AddPasswordAuthenticationCmd cmd, IDocumentSession documentSession, Session session)
    {
        var existingUser = await documentSession.Query<User>()
            .Where(u => u.UserId == cmd.UserId)
            .FirstOrDefaultAsync();

        if (existingUser == null)
            return new CommandResult(cmd,false, null, "User does not exist");

        if (existingUser.Password != null)
            return new CommandResult(cmd, false,null, "User already has a password authentication");

        var passwordHash = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cmd.Password)));

        documentSession.Events.Append(cmd.UserId,
            new PasswordAuthenticationAddedEvent(session.SessionId, cmd.UserId, passwordHash));
        return new CommandResult(cmd,true);
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new AddPasswordAuthenticationCmdHandler(serviceProvider));
}
