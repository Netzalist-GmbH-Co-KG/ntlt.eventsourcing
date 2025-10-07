using FluentValidation;
using Marten;
using ntlt.eventsourcing.core.Common;

namespace ntlt.eventsourcing.core.EventSourcing;

/// <summary>
///     Base class for command services.
///     Replaces actor-based command handlers with simple async services.
/// </summary>
public abstract class CommandServiceBase
{
    protected CommandServiceBase(IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider, IGuidProvider guidProvider, ILogger logger)
    {
        ServiceProvider = serviceProvider;
        DateTimeProvider = dateTimeProvider;
        GuidProvider = guidProvider;
        Logger = logger;
    }

    protected IServiceProvider ServiceProvider { get; }
    protected IDateTimeProvider DateTimeProvider { get; }
    protected IGuidProvider GuidProvider { get; }
    protected ILogger Logger { get; }

    /// <summary>
    ///     Execute command without Marten session context
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

            // Validate command
            var validator = scope.ServiceProvider.GetService<IValidator<TCmd>>();
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(cmd);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    Logger.LogWarning("Command {CommandName} validation failed: {Errors}", commandName, errors);
                    return new CommandResult(cmd, false, null, errors);
                }
            }

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
}