using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.EndSession;

public record EndSessionCmd(Guid SessionId, string Reason) : ICmd;

public record EndSessionResult(bool Success, string? ErrorMessage = null);