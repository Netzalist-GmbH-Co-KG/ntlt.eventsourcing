using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.UserManagement.Cmd;

// Cmd
public record CreateUserCmd(Guid? SessionId, string UserName, string Email) : ICmd;

// Events
public record UserCreatedEvent(Guid SessionId, Guid UserId, string UserName, string Email, DateTime CreatedAt)
    : IDomainEvent;