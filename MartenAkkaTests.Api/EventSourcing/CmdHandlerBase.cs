using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.EventSourcing;

public abstract class CmdHandlerBase : ReceiveActor
{
    protected IServiceProvider ServiceProvider { get; }
    protected IDateTimeProvider DateTimeProvider { get; }
    protected IGuidProvider GuidProvider { get; }

    protected CmdHandlerBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();
        GuidProvider = serviceProvider.GetRequiredService<IGuidProvider>();
    }
    
    protected async Task<bool> IsValidSession(Guid sessionId, IDocumentSession session)
    {
        return await session.Query<Session>()
            .Where(s => s.SessionId == sessionId && !s.Closed)
            .AnyAsync();
    }

    protected async Task HandleCmdInSession<T>(Func<T, IDocumentSession, Task<CommandResult>> handler, T cmd) where T : ICmd
    {
        try
        {
            await TryHandleCmdInSession(handler, cmd);
        }
        catch (Exception e) 
        {
            Sender.Tell(new CommandResult(cmd, false, null, $"Internal error: {e.Message}"));
        }
    }
    
    protected async Task HandleCmdInSession<T>(Func<T, IDocumentSession, Session, Task<CommandResult>> handler, T cmd, bool validateSession=true) where T : ICmd
    {
        try
        {
            await TryHandleCmdInSession(handler, cmd);
        }
        catch (Marten.Exceptions.MartenCommandException ex)
            when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            Sender.Tell(new CommandResult(cmd, false,null, "Race condition: Unique constraint violated"));
        }
        catch (Exception e) 
        {
            Sender.Tell(new CommandResult(cmd, false, null, $"Internal error: {e.Message}"));
        }
    }

    private async Task TryHandleCmdInSession<T>(Func<T, IDocumentSession, Task<CommandResult>> handler, T cmd) where T : ICmd
    {
        using var scope = ServiceProvider.CreateScope();
        var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

        // Execute the handler
        var result = await handler(cmd, documentSession);
        if (result.Success)
        {
            await documentSession.SaveChangesAsync();
        }
        Sender.Tell(result);
    }
    
    
    private async Task TryHandleCmdInSession<T>(Func<T, IDocumentSession, Session, Task<CommandResult>> handler, T cmd) where T : ICmd
    {
        using var scope = ServiceProvider.CreateScope();
        var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

        if (cmd.SessionId==null)
        {
            Sender.Tell(new CommandResult(cmd, false, null, "SessionId is missing"));
            return;
        }

        var session = await documentSession.Query<Session>()
            .Where(s => s.SessionId == cmd.SessionId.Value)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            Sender.Tell(new CommandResult(cmd, false, null, "Invalid SessionId"));
            return;
        }

        if (session.Closed)
        {
            Sender.Tell(new CommandResult(cmd, false, null, "Session is closed"));
            return;
        }

        // Execute the handler
        var result = await handler(cmd, documentSession, session!);
        if (!result.Success)
        {
            documentSession.EjectAllPendingChanges();
        }

        await documentSession.SaveChangesAsync();
        Sender.Tell(result);
    }
}