namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
///     Command to rebuild projections
/// </summary>
public sealed record RebuildProjectionCommand(Guid? SessionId, string? ProjectionName = null) : ICmd;