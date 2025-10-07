using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId);