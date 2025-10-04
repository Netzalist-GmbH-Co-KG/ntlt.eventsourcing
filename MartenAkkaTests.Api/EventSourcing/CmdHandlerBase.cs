using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.EventSourcing;

public abstract class CmdHandlerBase : ReceiveActor
{
    protected IServiceProvider ServiceProvider { get; }
    protected IDateTimeProvider DateTimeProvider { get; }
    protected IGuidProvider GuidProvider { get; }

    protected CmdHandlerBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();
        GuidProvider = serviceProvider.GetRequiredService<IGuidProvider>();
    }
    
    protected async Task<bool> IsValidSession(Guid sessionId, IDocumentSession session)
    {
        return await session.Query<Session>()
            .Where(s => s.SessionId == sessionId && !s.Closed)
            .AnyAsync();
    }
}