using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.SessionManagement.Cmd;

// Cmd
public record EndSessionCmd(Guid? SessionId, string Reason) : ICmd;

// Event
public record SessionEndedEvent(Guid SessionId, string Reason, DateTime EndedAt) : IDomainEvent;

// Handler
public class EndSessionCmdHandler : ICommandHandler<EndSessionCmd>
{
    public async Task<CommandResult> Handle(
        EndSessionCmd cmd,
        IDocumentSession session,
        Session sessionObj,
        IDateTimeProvider dateTimeProvider,
        IGuidProvider guidProvider)
    {
        session.Events.Append(sessionObj.SessionId,
            new SessionEndedEvent(sessionObj.SessionId, cmd.Reason, dateTimeProvider.UtcNow));

        return await Task.FromResult(new CommandResult(cmd, true));
    }
}

