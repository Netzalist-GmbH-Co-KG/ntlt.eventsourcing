namespace MartenAkkaTests.Api.Common;

public class GuidProvider : IGuidProvider
{
    public Guid NewGuid()  => Guid.NewGuid();
}