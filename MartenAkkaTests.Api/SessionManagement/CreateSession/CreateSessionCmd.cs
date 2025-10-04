using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.CreateSession;

public record CreateSessionCmd : ICmd;

public record CreateSessionResult(bool Success, Guid? SessionId = null, string? ErrorMessage = null);