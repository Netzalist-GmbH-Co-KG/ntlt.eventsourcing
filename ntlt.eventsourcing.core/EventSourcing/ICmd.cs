namespace ntlt.eventsourcing.core.EventSourcing;

// Marker interface for commands
public interface ICmd
{
    Guid? SessionId { get; }
}