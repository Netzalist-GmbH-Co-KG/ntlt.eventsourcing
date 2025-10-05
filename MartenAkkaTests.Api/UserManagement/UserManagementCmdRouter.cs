using Akka.Actor;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.UserManagement.Cmd;

namespace MartenAkkaTests.Api.UserManagement;

public class UserManagementCmdRouter : CmdRouterBase
{
    public UserManagementCmdRouter(IServiceProvider serviceProvider)
    {
        ForwardMessage<CreateUserCmd>(CreateUserCmdHandler.Prop(serviceProvider));
        ForwardMessage<DeactivateUserCmd>(DeactivateUserCmdHandler.Prop(serviceProvider));
        ForwardMessage<AddPasswordAuthenticationCmd>(AddPasswordAuthenticationCmdHandler.Prop(serviceProvider));
    }
    
    public static Props Prop(IServiceProvider serviceProvider) =>
        Props.Create(() => new UserManagementCmdRouter(serviceProvider));
}