using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.SessionManagement.Cmd;

// Cmd
public record EndSessionCmd(Guid? SessionId, Guid SessionToEndId, string Reason) : ICmd;

// Event
public record SessionEndedEvent(Guid SessionId, Guid? EndedSessionId, string Reason, DateTime EndedAt);