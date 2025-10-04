using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement.SessionActivity;
using MartenAkkaTests.Api.UserManagement.CreateUser;

namespace MartenAkkaTests.Api.UserManagement.DeactivateUser;

public class DeactivateUserCmdHandler : CmdHandlerBase
{
    public DeactivateUserCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<DeactivateUserCmd>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(DeactivateUserCmd cmd)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var isValidSession = await IsValidSession(cmd.SessionId, session);
            if (!isValidSession)
            {
                Sender.Tell(new CreateUserResult(false, null, "Invalid or closed session"));
                return;
            }

            var existingUser = await session.Query<User>()
                .Where(u => u.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                Sender.Tell(new DeactivateUserResult(false, "User not found"));
                return;
            }

            if (existingUser.IsDeactivated)
            {
                // Idempotent - already deactivated
                Sender.Tell(new DeactivateUserResult(true));
                return;
            }

            session.Events.Append(cmd.UserId,
                new UserDeactivatedEvent(cmd.SessionId, cmd.UserId));
            session.Events.Append(cmd.SessionId,
                new SessionActivityRecordedEvent(cmd.SessionId, DateTimeProvider.UtcNow));
            await session.SaveChangesAsync();
            Sender.Tell(new DeactivateUserResult(true));
        }
        catch (Exception e)
        {
            Sender.Tell(new CreateUserResult(false, null,$"Internal error: {e.Message}"));
        }
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new DeactivateUserCmdHandler(serviceProvider));
}
