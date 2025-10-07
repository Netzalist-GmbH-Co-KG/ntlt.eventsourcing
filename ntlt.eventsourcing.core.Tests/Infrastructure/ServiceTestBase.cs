using Microsoft.Extensions.Logging;
using NSubstitute;
using ntlt.eventsourcing.core.Common;

namespace ntlt.eventsourcing.core.Tests.Infrastructure;

public abstract class ServiceTestBase
{
    protected IDateTimeProvider DateTimeProvider { get; private set; } = null!;
    protected IGuidProvider GuidProvider { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;

    [SetUp]
    public void BaseSetup()
    {
        DateTimeProvider = new FakeDateTimeProvider();
        GuidProvider = new FakeGuidProvider();
        Logger = Substitute.For<ILogger>();
    }
}
