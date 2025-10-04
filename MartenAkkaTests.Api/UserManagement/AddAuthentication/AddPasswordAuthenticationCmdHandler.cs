using System.Security.Cryptography;

namespace MartenAkkaTests.Api.UserManagement.AddAuthentication;

using Akka.Actor;
using Marten;
using EventSourcing;
using SessionManagement.SessionActivity;

public class AddPasswordAuthenticationCmdHandler : CmdHandlerBase
{
    public AddPasswordAuthenticationCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<AddPasswordAuthenticationCmd>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(AddPasswordAuthenticationCmd cmd)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var isValidSession = await IsValidSession(cmd.SessionId, session);
            if (!isValidSession)
            {
                Sender.Tell(new AddPasswordAuthenticationResult(false, "Invalid or closed session"));
                return;
            }
            
            var existingUser = await session.Query<User>()
                .Where(u => u.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                Sender.Tell(new AddPasswordAuthenticationResult(false, "User does not exist"));
                return;
            }

            if (existingUser.Password != null)
            {
                Sender.Tell(new AddPasswordAuthenticationResult(false, "User already has a password authentication"));
                return;
            }

            var passwordHash = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cmd.Password)));
            session.Events.Append(cmd.UserId,
                new PasswordAuthenticationAddedEvent(cmd.SessionId, cmd.UserId, passwordHash));
            session.Events.Append(cmd.SessionId,
                new SessionActivityRecordedEvent(cmd.SessionId, DateTimeProvider.UtcNow));
            await session.SaveChangesAsync();
            Sender.Tell(new AddPasswordAuthenticationResult(true));
        }

        catch (Exception e)
        {
            Sender.Tell(new AddPasswordAuthenticationResult(false, $"Internal error: {e.Message}"));
        }
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new AddPasswordAuthenticationCmdHandler(serviceProvider));
}
