using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.CreateSession;

public class CreateSessionCmdHandler : CmdHandlerBase
{
    public CreateSessionCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<CreateSessionCmd>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(CreateSessionCmd cmd)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var sessionId = GuidProvider.NewGuid();
            session.Events.StartStream<Session>(sessionId,
                new SessionCreatedEvent(sessionId, DateTimeProvider.UtcNow));
            await session.SaveChangesAsync();
            Sender.Tell(new CreateSessionResult(true, sessionId));
        }
        catch (Exception e)
        {
            Sender.Tell(new CreateSessionResult(false, null,$"Internal error: {e.Message}"));
        }
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new CreateSessionCmdHandler(serviceProvider));
}