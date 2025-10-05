using Akka.Actor;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;

namespace MartenAkkaTests.Api.EventSourcing;

public abstract class CmdRouterBase : ReceiveActor
{
    protected void ForwardMessage<T>(Props props)
    {
        Receive<T>( cmd =>
        {
            var handler = Context.ActorOf(props);
            handler.Forward(cmd);
        });
    }
}