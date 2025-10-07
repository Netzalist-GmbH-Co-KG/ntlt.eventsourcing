using ntlt.eventsourcing.Api.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ntlt.eventsourcing.Api.Tests.Infrastructure;

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
