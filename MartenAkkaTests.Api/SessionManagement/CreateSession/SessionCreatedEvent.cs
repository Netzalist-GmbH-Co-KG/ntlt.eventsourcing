using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.CreateSession;

public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;