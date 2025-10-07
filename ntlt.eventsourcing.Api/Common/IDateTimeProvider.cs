namespace ntlt.eventsourcing.Api.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}