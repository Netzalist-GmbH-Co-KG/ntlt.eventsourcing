using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId);