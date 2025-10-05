using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement.Cmd;

namespace MartenAkkaTests.Api.SessionManagement;

/// <summary>
/// Command service for session management operations.
/// Handlers are registered in constructor and executed via Handle() method.
/// </summary>
public class SessionCommandService : CommandServiceBase
{
    private readonly Dictionary<Type, object> _handlers = new();

    public SessionCommandService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        RegisterHandler(new CreateSessionCmdHandler());
        RegisterHandler(new EndSessionCmdHandler());
    }

    private void RegisterHandler<TCmd>(ICommandHandler<TCmd> handler) where TCmd : ICmd
    {
        _handlers[typeof(TCmd)] = handler;
    }

    public async Task<CommandResult> Handle<TCmd>(TCmd cmd) where TCmd : ICmd
    {
        if (!_handlers.TryGetValue(typeof(TCmd), out var handlerObj))
            throw new InvalidOperationException($"No handler registered for command type {typeof(TCmd).Name}");

        var handler = (ICommandHandler<TCmd>)handlerObj;

        // CreateSessionCmd doesn't require session validation (session doesn't exist yet)
        if (cmd is CreateSessionCmd)
        {
            return await ExecuteCommand(cmd, async (cmd, session) =>
            {
                // Pass null for sessionObj since it doesn't exist yet
                return await handler.Handle(cmd, session, null!, DateTimeProvider, GuidProvider);
            });
        }

        // All other commands require session validation
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            return await handler.Handle(cmd, session, sessionObj, DateTimeProvider, GuidProvider);
        });
    }
}
