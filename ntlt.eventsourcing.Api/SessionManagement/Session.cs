using Marten.Events.Aggregation;
using ntlt.eventsourcing.Api.SessionManagement.Cmd;
using ntlt.eventsourcing.Api.SessionManagement.Evt;

namespace ntlt.eventsourcing.Api.SessionManagement;

// Aggregate
public record Session(Guid SessionId, DateTime CreatedAt, DateTime LastAccessedAt, bool Closed);

// Projection
public class SessionProjection : SingleStreamProjection<Session, Guid>
{
    public Session Create(SessionCreatedEvent evt)
    {
        return new Session(evt.SessionId, evt.CreatedAt, evt.CreatedAt, false);
    }

    public Session Apply(Session session, SessionActivityRecordedEvent evt)
    {
        return session with { LastAccessedAt = evt.AccessedAt };
    }

    public Session Apply(Session session, SessionEndedEvent evt)
    {
        return session with { LastAccessedAt = evt.EndedAt, Closed = true };
    }
}