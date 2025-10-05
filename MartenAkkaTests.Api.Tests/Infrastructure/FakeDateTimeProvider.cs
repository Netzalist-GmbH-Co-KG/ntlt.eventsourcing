using MartenAkkaTests.Api.Common;

namespace MartenAkkaTests.Api.Tests.Infrastructure;

public class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _fakeDateTime;

    public FakeDateTimeProvider(DateTime? initialDateTime = null)
    {
        _fakeDateTime = initialDateTime ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow => _fakeDateTime.ToUniversalTime();

    public void SetTime(DateTime dateTime)
    {
        _fakeDateTime = dateTime;
    }

    public void AdvanceTime(TimeSpan timeSpan)
    {
        _fakeDateTime = _fakeDateTime.Add(timeSpan);
    }
}