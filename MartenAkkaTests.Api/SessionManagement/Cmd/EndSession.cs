using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record EndSessionCmd(Guid? SessionId, string Reason) : ICmd;

// Event
public record SessionEndedEvent(Guid SessionId, string Reason, DateTime EndedAt);