namespace MartenAkkaTests.Api.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}