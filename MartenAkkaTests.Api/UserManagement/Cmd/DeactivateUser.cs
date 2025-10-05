using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId);