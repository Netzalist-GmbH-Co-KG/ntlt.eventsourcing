using MartenAkkaTests.Api.Common;

namespace MartenAkkaTests.Api.Tests.Infrastructure;

public class FakeGuidProvider : IGuidProvider
{
    private Queue<Guid> _guids;
    private Guid _lastFakeGuid;

    public FakeGuidProvider()
    {
        _guids = new Queue<Guid>(AllTestGuids);
    }

    public static Guid Guid1 => new("00000000-0000-0000-0000-000000000001");
    public static Guid Guid2 => new("00000000-0000-0000-0000-000000000002");
    public static Guid Guid3 => new("00000000-0000-0000-0000-000000000003");
    public static Guid Guid4 => new("00000000-0000-0000-0000-000000000004");
    public static Guid Guid5 => new("00000000-0000-0000-0000-000000000005");
    public static Guid Guid6 => new("00000000-0000-0000-0000-000000000006");
    public static Guid Guid7 => new("00000000-0000-0000-0000-000000000007");
    public static Guid Guid8 => new("00000000-0000-0000-0000-000000000008");
    public static Guid Guid9 => new("00000000-0000-0000-0000-000000000009");

    public static List<Guid> AllTestGuids => [Guid1, Guid2, Guid3, Guid4, Guid5, Guid6, Guid7, Guid8, Guid9];

    public Guid NewGuid()
    {
        if (_guids.Count > 0) _lastFakeGuid = _guids.Dequeue();
        return _lastFakeGuid;
    }

    public void SetFakeGuids(IEnumerable<Guid> guids)
    {
        _guids = new Queue<Guid>(guids);
    }
}