using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.SessionActivity;

public record SessionActivityRecordedEvent(Guid SessionId, DateTime AccessedAt) : IDomainEvent;
