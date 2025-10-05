using Marten;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement.Cmd;

namespace MartenAkkaTests.Api.SessionManagement;

/// <summary>
/// Command service for session management operations.
/// Replaces SessionManagementCmdRouter and session command handler actors.
/// </summary>
public class SessionCommandService : CommandServiceBase
{
    public SessionCommandService(IServiceProvider serviceProvider, ILogger<SessionCommandService> logger)
        : base(serviceProvider, logger)
    {
    }

    public async Task<CommandResult> CreateSession(CreateSessionCmd cmd)
    {
        return await ExecuteCommand(cmd, async (cmd, session) =>
        {
            var sessionId = GuidProvider.NewGuid();
            session.Events.StartStream<Session>(sessionId,
                new SessionCreatedEvent(sessionId, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, sessionId);
        });
    }

    public async Task<CommandResult> EndSession(EndSessionCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            session.Events.Append(sessionObj.SessionId,
                new SessionEndedEvent(sessionObj.SessionId, cmd.Reason, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true);
        });
    }
}
