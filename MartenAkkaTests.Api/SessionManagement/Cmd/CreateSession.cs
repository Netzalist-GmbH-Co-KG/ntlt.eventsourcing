using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record CreateSessionCmd(Guid? SessionId = null) : ICmd;

// Events
public record SessionCreatedEvent(Guid SessionId, DateTime CreatedAt) : IDomainEvent;

// Handler
public class CreateSessionCmdHandler : ICommandHandler<CreateSessionCmd>
{
    public async Task<CommandResult> Handle(
        CreateSessionCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider)
    {
        var sessionId = guidProvider.NewGuid();
        session.Events.StartStream<Session>(sessionId,
            new SessionCreatedEvent(sessionId, dateTimeProvider.UtcNow));

        return await Task.FromResult(new CommandResult(cmd, true, sessionId));
    }
}

