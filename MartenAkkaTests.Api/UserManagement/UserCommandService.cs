using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.UserManagement.Cmd;

namespace MartenAkkaTests.Api.UserManagement;

/// <summary>
/// Command service for user management operations.
/// Handlers are registered in constructor and executed via Handle() method.
/// </summary>
public class UserCommandService : CommandServiceBase
{
    private readonly Dictionary<Type, object> _handlers = new();

    public UserCommandService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        RegisterHandler(new CreateUserCmdHandler());
        RegisterHandler(new AddPasswordAuthenticationCmdHandler());
        RegisterHandler(new DeactivateUserCmdHandler());
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

        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            return await handler.Handle(cmd, session, sessionObj, DateTimeProvider, GuidProvider);
        });
    }
}
