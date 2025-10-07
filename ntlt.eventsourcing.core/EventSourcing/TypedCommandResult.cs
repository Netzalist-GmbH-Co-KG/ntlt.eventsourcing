namespace ntlt.eventsourcing.core.EventSourcing;

/// <summary>
///     Type-safe discriminated union for command execution results.
///     Provides type-safe result handling with pattern matching.
///     This will replace CommandResult in the future.
/// </summary>
public abstract record TypedCommandResult
{
    private TypedCommandResult()
    {
    }

    /// <summary>
    ///     Command executed successfully without return data
    /// </summary>
    public sealed record Success : TypedCommandResult;

    /// <summary>
    ///     Command executed successfully with typed return data
    /// </summary>
    public sealed record Success<T>(T Data) : TypedCommandResult;

    /// <summary>
    ///     Command execution failed with error details
    /// </summary>
    public sealed record Failure(string ErrorCode, string Message) : TypedCommandResult;
}

/// <summary>
///     Extension methods for working with TypedCommandResult
/// </summary>
public static class TypedCommandResultExtensions
{
    /// <summary>
    ///     Pattern match on TypedCommandResult
    /// </summary>
    public static TResult Match<TResult>(
        this TypedCommandResult result,
        Func<TResult> onSuccess,
        Func<string, string, TResult> onFailure)
    {
        return result switch
        {
            TypedCommandResult.Success => onSuccess(),
            TypedCommandResult.Failure f => onFailure(f.ErrorCode, f.Message),
            _ => throw new InvalidOperationException($"Unexpected TypedCommandResult type: {result.GetType()}")
        };
    }

    /// <summary>
    ///     Pattern match on TypedCommandResult with typed success data
    /// </summary>
    public static TResult Match<TData, TResult>(
        this TypedCommandResult result,
        Func<TData, TResult> onSuccess,
        Func<string, string, TResult> onFailure)
    {
        return result switch
        {
            TypedCommandResult.Success<TData> s => onSuccess(s.Data),
            TypedCommandResult.Failure f => onFailure(f.ErrorCode, f.Message),
            _ => throw new InvalidOperationException($"Unexpected TypedCommandResult type: {result.GetType()}")
        };
    }

    /// <summary>
    ///     Check if result is successful
    /// </summary>
    public static bool IsSuccess(this TypedCommandResult result)
    {
        return result is TypedCommandResult.Success or TypedCommandResult.Success<object>;
    }

    /// <summary>
    ///     Check if result is failure
    /// </summary>
    public static bool IsFailure(this TypedCommandResult result)
    {
        return result is TypedCommandResult.Failure;
    }

    /// <summary>
    ///     Get data from Success result or throw
    /// </summary>
    public static T GetData<T>(this TypedCommandResult result)
    {
        if (result is TypedCommandResult.Success<T> success)
            return success.Data;

        throw new InvalidOperationException(
            result is TypedCommandResult.Failure f
                ? $"Cannot get data from failed result: {f.Message}"
                : "Cannot get data from non-typed success result");
    }

    /// <summary>
    ///     Convert legacy CommandResult to new TypedCommandResult
    /// </summary>
    public static TypedCommandResult ToTyped(this CommandResult legacy)
    {
        if (legacy.Success)
            return legacy.ResultData != null
                ? new TypedCommandResult.Success<object>(legacy.ResultData)
                : new TypedCommandResult.Success();

        return new TypedCommandResult.Failure(
            "COMMAND_FAILED",
            legacy.ErrorMessage ?? "Command execution failed");
    }

    /// <summary>
    ///     Convert TypedCommandResult to legacy CommandResult (for backward compat)
    /// </summary>
    public static CommandResult ToLegacy(this TypedCommandResult result, ICmd cmd)
    {
        return result switch
        {
            TypedCommandResult.Success => new CommandResult(cmd, true),
            TypedCommandResult.Success<object> s => new CommandResult(cmd, true, s.Data),
            TypedCommandResult.Failure f => new CommandResult(cmd, false, null, f.Message),
            _ => new CommandResult(cmd, false, null, "Unknown result type")
        };
    }
}