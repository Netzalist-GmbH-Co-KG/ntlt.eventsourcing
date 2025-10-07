using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.SessionManagement.Cmd;

// Cmd
public record EndSessionCmd(Guid? SessionId, Guid SessionToEndId, string Reason) : ICmd;

// Event
public record SessionEndedEvent(Guid SessionId, Guid? EndedSessionId, string Reason, DateTime EndedAt);