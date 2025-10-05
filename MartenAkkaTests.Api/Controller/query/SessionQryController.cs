using Marten;
using MartenAkkaTests.Api.SessionManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.query;

public class SessionQryController : ControllerBase
{
    private readonly IDocumentStore _documentStore;

    public SessionQryController(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    [HttpGet("api/query/session/list")]
    public async Task<IActionResult> GetAllSessions()
    {
        await using var session = _documentStore.LightweightSession();
        var sessions = await session.Query<Session>()
            .Select(s => new { s.SessionId, s.CreatedAt, s.LastAccessedAt, s.Closed })
            .ToListAsync();

        return Ok(sessions);
    }
}