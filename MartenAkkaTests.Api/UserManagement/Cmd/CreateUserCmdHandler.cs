using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;

// Cmd Handler
public class CreateUserCmdHandler : CmdHandlerBase
{
    public CreateUserCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<CreateUserCmd>(async cmd => await HandleCmdInSession(HandleMessage, cmd));
    }

    private async Task<CommandResult> HandleMessage(CreateUserCmd cmd, IDocumentSession documentSession, Session session)
    {
            var existingUser = await documentSession.Query<User>()
                .Where(u => u.UserName == cmd.UserName || u.Email == cmd.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return existingUser.UserName == cmd.UserName 
                    ? new CommandResult(cmd, false, null,"Username already exists") 
                    : new CommandResult(cmd, false, null, "Email already exists");
            }

            var userId = GuidProvider.NewGuid();
            documentSession.Events.StartStream<User>(userId,
                new UserCreatedEvent(session.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new CreateUserCmdHandler(serviceProvider));
}
