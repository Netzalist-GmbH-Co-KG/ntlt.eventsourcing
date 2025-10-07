namespace ntlt.eventsourcing.Api.SessionManagement;

/// <summary>
///     Session activity tracking - CRUD table (not event sourced).
///     Tracks last access time for sessions without polluting event stream.
/// </summary>
public record SessionActivity(Guid SessionId, DateTime LastAccessedAt)
{
    /// <summary>
    ///     Marten identity property
    /// </summary>
    public Guid Id => SessionId;
}