namespace ntlt.eventsourcing.core.Common;

public class GuidProvider : IGuidProvider
{
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }
}