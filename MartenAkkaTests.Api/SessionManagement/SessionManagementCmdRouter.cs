using Akka.Actor;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement.Cmd;
namespace MartenAkkaTests.Api.SessionManagement;

public class SessionManagementCmdRouter : CmdRouterBase
{
    public SessionManagementCmdRouter(IServiceProvider serviceProvider)
    {
        ForwardMessage<CreateSessionCmd>(CreateSessionCmdHandler.Prop(serviceProvider));
        ForwardMessage<EndSessionCmd>(EndSessionCmdHandler.Prop(serviceProvider));
    }
    
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new SessionManagementCmdRouter(serviceProvider));
}