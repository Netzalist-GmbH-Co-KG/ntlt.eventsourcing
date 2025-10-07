namespace ntlt.eventsourcing.core.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}