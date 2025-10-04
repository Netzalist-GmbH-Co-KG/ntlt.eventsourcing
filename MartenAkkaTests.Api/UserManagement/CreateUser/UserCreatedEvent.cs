using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.UserManagement.CreateUser;

public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;