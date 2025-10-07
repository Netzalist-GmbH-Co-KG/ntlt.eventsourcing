using ntlt.eventsourcing.Api.EventSourcing;

namespace ntlt.eventsourcing.Api.UserManagement.Cmd;

// Cmd
public record AddPasswordAuthenticationCmd(Guid? SessionId, Guid UserId, string Password) : ICmd;

// Events
public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash);