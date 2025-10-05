namespace MartenAkkaTests.Api.EventSourcing;

public record CommandResult(ICmd Cmd, bool Success, object? ResultData = null, string? ErrorMessage = null);