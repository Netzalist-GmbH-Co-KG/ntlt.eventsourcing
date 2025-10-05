using Marten;
using Marten.Exceptions;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using MartenAkkaTests.Api.SessionManagement;
using Npgsql;

namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
///     Base class for command services.
///     Replaces actor-based command handlers with simple async services.
/// </summary>
public abstract class CommandServiceBase
{
    protected CommandServiceBase(IServiceProvider serviceProvider, ILogger logger)
    {
        ServiceProvider = serviceProvider;
        DateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();
        GuidProvider = serviceProvider.GetRequiredService<IGuidProvider>();
        Logger = logger;
    }

    protected IServiceProvider ServiceProvider { get; }
    protected IDateTimeProvider DateTimeProvider { get; }
    protected IGuidProvider GuidProvider { get; }
    protected ILogger Logger { get; }

    /// <summary>
    ///     Execute command without session validation.
    ///     Use for commands that don't require authentication (e.g., CreateSession).
    /// </summary>
    protected async Task<CommandResult> ExecuteCommand<TCmd>(
        TCmd cmd,
        Func<TCmd, IDocumentSession, Task<CommandResult>> handler)
        where TCmd : ICmd
    {
        var commandName = typeof(TCmd).Name;

        try
        {
            Logger.LogInformation("Executing command {CommandName}", commandName);

            using var scope = ServiceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var result = await handler(cmd, documentSession);

            if (result.Success)
            {
                await documentSession.SaveChangesAsync();
                Logger.LogInformation("Command {CommandName} executed successfully", commandName);
            }
            else
            {
                Logger.LogWarning("Command {CommandName} failed: {ErrorMessage}", commandName, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unexpected error executing command {CommandName}", commandName);
            return new CommandResult(cmd, false, null, "An error occurred processing your request");
        }
    }

    /// <summary>
    ///     Execute command with session validation and exception handling.
    ///     Session is retrieved from HttpContext (set by SessionValidationMiddleware).
    ///     Falls back to DB query if HttpContext is not available (e.g., in tests).
    /// </summary>
    protected async Task<CommandResult> ExecuteCommandInSession<TCmd>(
        TCmd cmd,
        Func<TCmd, IDocumentSession, Session, Task<CommandResult>> handler)
        where TCmd : ICmd
    {
        var commandName = typeof(TCmd).Name;

        try
        {
            Logger.LogInformation("Executing command {CommandName} in session {SessionId}", commandName, cmd.SessionId);

            using var scope = ServiceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            if (cmd.SessionId == null)
            {
                Logger.LogWarning("Command {CommandName} missing SessionId", commandName);
                return new CommandResult(cmd, false, null, "SessionId is missing");
            }

            // Try to get session from HttpContext (performance optimization)
            Session? session = null;
            try
            {
                var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
                if (httpContextAccessor?.HttpContext != null)
                {
                    session = httpContextAccessor.HttpContext.GetSession();
                    Logger.LogDebug("Session loaded from HttpContext for command {CommandName}", commandName);
                }
            }
            catch (InvalidOperationException)
            {
                // HttpContext not available or session not in context - fall back to DB query
            }

            // Fallback to DB query if not in HttpContext (e.g., in tests)
            if (session == null)
            {
                Logger.LogDebug("Loading session from database for command {CommandName}", commandName);
                session = await documentSession.Query<Session>()
                    .Where(s => s.SessionId == cmd.SessionId.Value)
                    .FirstOrDefaultAsync();

                if (session == null)
                {
                    Logger.LogWarning("Invalid SessionId {SessionId} for command {CommandName}", cmd.SessionId,
                        commandName);
                    return new CommandResult(cmd, false, null, "Invalid SessionId");
                }

                if (session.Closed)
                {
                    Logger.LogWarning("Session {SessionId} is closed for command {CommandName}", cmd.SessionId,
                        commandName);
                    return new CommandResult(cmd, false, null, "Session is closed");
                }
            }

            var result = await handler(cmd, documentSession, session);

            if (!result.Success)
            {
                documentSession.EjectAllPendingChanges();
                Logger.LogWarning("Command {CommandName} failed: {ErrorMessage}", commandName, result.ErrorMessage);
            }
            else
            {
                Logger.LogInformation("Command {CommandName} executed successfully in session {SessionId}", commandName,
                    cmd.SessionId);
            }

            await documentSession.SaveChangesAsync();

            return result;
        }
        catch (MartenCommandException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            Logger.LogWarning(ex, "Race condition detected in command {CommandName}: Unique constraint violated",
                commandName);
            return new CommandResult(cmd, false, null, "Race condition: Unique constraint violated");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unexpected error executing command {CommandName}", commandName);
            return new CommandResult(cmd, false, null, "An error occurred processing your request");
        }
    }
}