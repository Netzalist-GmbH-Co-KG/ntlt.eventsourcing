using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Evt;

// Event
public record SessionActivityRecordedEvent(Guid SessionId, DateTime AccessedAt) : IDomainEvent;
