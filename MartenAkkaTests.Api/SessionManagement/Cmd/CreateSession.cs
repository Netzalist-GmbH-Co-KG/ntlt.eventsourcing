using Marten;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record CreateSessionCmd(Guid? SessionId = null) : ICmd;

// Events
public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;

