using Marten;
using Marten.Exceptions;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.SessionManagement;
using Npgsql;

namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
/// Base class for command services.
/// Replaces actor-based command handlers with simple async services.
/// </summary>
public abstract class CommandServiceBase
{
    protected IServiceProvider ServiceProvider { get; }
    protected IDateTimeProvider DateTimeProvider { get; }
    protected IGuidProvider GuidProvider { get; }

    protected CommandServiceBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();
        GuidProvider = serviceProvider.GetRequiredService<IGuidProvider>();
    }

    /// <summary>
    /// Execute command with session validation and exception handling.
    /// Session object is NOT passed - must be retrieved via HttpContext if needed.
    /// </summary>
    protected async Task<CommandResult> ExecuteCommand<TCmd>(
        TCmd cmd,
        Func<TCmd, IDocumentSession, Task<CommandResult>> handler)
        where TCmd : ICmd
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var result = await handler(cmd, documentSession);

            if (result.Success)
            {
                await documentSession.SaveChangesAsync();
            }

            return result;
        }
        catch (Exception e)
        {
            return new CommandResult(cmd, false, null, $"Internal error: {e.Message}");
        }
    }

    /// <summary>
    /// Execute command with session validation and exception handling.
    /// Session is validated and passed to handler.
    /// </summary>
    protected async Task<CommandResult> ExecuteCommandInSession<TCmd>(
        TCmd cmd,
        Func<TCmd, IDocumentSession, Session, Task<CommandResult>> handler)
        where TCmd : ICmd
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            if (cmd.SessionId == null)
            {
                return new CommandResult(cmd, false, null, "SessionId is missing");
            }

            var session = await documentSession.Query<Session>()
                .Where(s => s.SessionId == cmd.SessionId.Value)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                return new CommandResult(cmd, false, null, "Invalid SessionId");
            }

            if (session.Closed)
            {
                return new CommandResult(cmd, false, null, "Session is closed");
            }

            var result = await handler(cmd, documentSession, session);

            if (!result.Success)
            {
                documentSession.EjectAllPendingChanges();
            }

            await documentSession.SaveChangesAsync();

            return result;
        }
        catch (MartenCommandException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return new CommandResult(cmd, false, null, "Race condition: Unique constraint violated");
        }
        catch (Exception e)
        {
            return new CommandResult(cmd, false, null, $"Internal error: {e.Message}");
        }
    }
}
