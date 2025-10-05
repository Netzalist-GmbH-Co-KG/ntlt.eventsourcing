using Marten.Events.Aggregation;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using MartenAkkaTests.Api.SessionManagement.Evt;

namespace MartenAkkaTests.Api.SessionManagement;

// Aggregate
public record Session(Guid SessionId, DateTime CreatedAt, DateTime LastAccessedAt, bool Closed);

// Projection
public class SessionProjection : SingleStreamProjection<Session, Guid>
{
    public Session Create(SessionCreatedEvent evt) =>
        new (evt.SessionId, evt.CreatedAt, evt.CreatedAt, false);
    
    public Session Apply(Session session, SessionActivityRecordedEvent evt) =>
        session with { LastAccessedAt = evt.AccessedAt };

    public Session Apply(Session session, SessionEndedEvent evt) =>
        session with { LastAccessedAt = evt.EndedAt, Closed = true };
}