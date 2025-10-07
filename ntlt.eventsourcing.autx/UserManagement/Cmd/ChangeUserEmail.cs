using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.UserManagement.Cmd;

// Command
public record ChangeUserEmailCmd(Guid? SessionId, Guid UserId, string NewEmail) : ICmd;

// Event
public record UserEmailChangedEvent(Guid SessionId, Guid UserId, string NewEmail, DateTime Timestamp) : IDomainEvent;
