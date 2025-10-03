using Marten.Events.Aggregation;
using MartenAkkaTests.Api.Controller;

namespace MartenAkkaTests.Api;

public sealed record SomethingCounter(Guid Id, int Count);

public class SomethingCounterProjection : SingleStreamProjection<SomethingCounter, Guid>
{
    public SomethingCounter Create(SomethingHappened started)
    {
        return new SomethingCounter(started.Id, 1);
    }

    public SomethingCounter Apply(SomethingCounter counter, SomethingHappened happened)
    {
        return counter with
        {
            Count = counter.Count + 1
        };
    }
}