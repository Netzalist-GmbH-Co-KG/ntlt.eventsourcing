using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.SessionManagement.Evt;

// Event
public record SessionActivityRecordedEvent(Guid SessionId, DateTime AccessedAt) : IDomainEvent;