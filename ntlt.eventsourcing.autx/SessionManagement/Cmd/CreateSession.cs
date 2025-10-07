using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.SessionManagement.Cmd;

// Cmd
public record CreateSessionCmd(Guid? SessionId = null) : ICmd;

// Events
public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;