namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
///     Legacy CommandResult format - kept for backward compatibility during migration.
///     Will be phased out in favor of TypedCommandResult.
/// </summary>
public record CommandResult(ICmd Cmd, bool Success, object? ResultData = null, string? ErrorMessage = null);