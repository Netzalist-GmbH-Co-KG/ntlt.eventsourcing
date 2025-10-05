using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.UserManagement.Cmd;

// Cmd
public record DeactivateUserCmd(Guid? SessionId, Guid UserId) : ICmd;

// Events
public record UserDeactivatedEvent(Guid SessionId, Guid UserId);

