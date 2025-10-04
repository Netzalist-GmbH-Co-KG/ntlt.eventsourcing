using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.SessionActivity;

namespace MartenAkkaTests.Api.UserManagement.CreateUser;

public class CreateUserCmdHandler : CmdHandlerBase
{
    public CreateUserCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<CreateUserCmd>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(CreateUserCmd cmd)
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
            
            var existingByUser = await session.Query<User>()
                .Where(u => u.UserName == cmd.UserName || u.Email == cmd.Email)
                .AnyAsync();

            if (existingByUser)
            {
                Sender.Tell(new CreateUserResult(false, null,"Username or Email already exists"));
                return;
            }

            var userId = GuidProvider.NewGuid();
            session.Events.StartStream<User>(userId,
                new UserCreatedEvent(cmd.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));
            session.Events.Append(cmd.SessionId,
                new SessionActivityRecordedEvent(cmd.SessionId, DateTimeProvider.UtcNow));
            await session.SaveChangesAsync();
            Sender.Tell(new CreateUserResult(true, userId));
        }
        catch (Marten.Exceptions.MartenCommandException ex)
            when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Unique constraint violated (Race condition trotz Check)
            Sender.Tell(new CreateUserResult(false,null, "Username or Email already exists"));
        }
        catch (Exception e)
        {
            Sender.Tell(new CreateUserResult(false, null,$"Internal error: {e.Message}"));
        }
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new CreateUserCmdHandler(serviceProvider));
}
