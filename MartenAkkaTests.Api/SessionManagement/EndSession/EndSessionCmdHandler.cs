using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.EndSession;

public class EndSessionCmdHandler : CmdHandlerBase
{
    public EndSessionCmdHandler(IServiceProvider serviceProvider): base(serviceProvider)
    {
        ReceiveAsync<EndSessionCmd>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(EndSessionCmd cmd)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
            
            var isValidSession = await IsValidSession(cmd.SessionId, session);
            
            if(!isValidSession)
            {
                Sender.Tell(new EndSessionResult(false, "Invalid or already closed session"));
                return;
            }

            session.Events.Append(cmd.SessionId,
                new SessionEndedEvent(cmd.SessionId, cmd.Reason, DateTimeProvider.UtcNow));

            await session.SaveChangesAsync();
            Sender.Tell(new EndSessionResult(true));
        }
        catch (Exception e)
        {
            Sender.Tell(new EndSessionResult(false,$"Internal error: {e.Message}"));
        }
    }
   
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new EndSessionCmdHandler(serviceProvider));
}