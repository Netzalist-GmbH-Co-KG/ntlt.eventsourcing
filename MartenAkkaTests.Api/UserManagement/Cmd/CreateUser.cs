using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt) : IDomainEvent;

