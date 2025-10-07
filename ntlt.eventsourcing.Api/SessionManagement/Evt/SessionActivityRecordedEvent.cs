using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.SessionManagement.Evt;

// Event
public record SessionActivityRecordedEvent(Guid SessionId, DateTime AccessedAt) : IDomainEvent;