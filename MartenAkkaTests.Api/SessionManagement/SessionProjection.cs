using Marten.Events.Aggregation;
using MartenAkkaTests.Api.SessionManagement.CreateSession;
using MartenAkkaTests.Api.SessionManagement.SessionActivity;

namespace MartenAkkaTests.Api.SessionManagement;

public class SessionProjection : SingleStreamProjection<Session, Guid>
{
    public Session Create(SessionCreatedEvent evt) =>
        new (evt.SessionId, evt.CreatedAt, evt.CreatedAt, false);
    
    public Session Apply(Session session, SessionActivityRecordedEvent evt) =>
        session with { LastAccessedAt = evt.AccessedAt };

}