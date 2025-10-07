using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.UserManagement.Cmd;

// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt)
    : IDomainEvent;