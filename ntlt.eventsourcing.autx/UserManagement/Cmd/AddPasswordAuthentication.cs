using ntlt.eventsourcing.core.EventSourcing;

namespace ntlt.eventsourcing.autx.UserManagement.Cmd;

// Cmd
public record AddPasswordAuthenticationCmd(Guid? SessionId, Guid UserId, string Password) : ICmd;

// Events
public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash);