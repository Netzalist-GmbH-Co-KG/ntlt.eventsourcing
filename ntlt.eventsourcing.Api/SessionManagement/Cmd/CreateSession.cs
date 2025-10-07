using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.SessionManagement.Cmd;

// Cmd
public record CreateSessionCmd(Guid? SessionId = null) : ICmd;

// Events
public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;