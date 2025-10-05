using Marten;
using MartenAkkaTests.Api.SessionManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.qry;

public class SessionController : V1QueryControllerBase
{
    private readonly IDocumentStore _documentStore;

    public SessionController(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        await using var session = _documentStore.LightweightSession();
        var sessions = await session.Query<Session>()
            .Select(s => new { s.SessionId, s.CreatedAt, s.LastAccessedAt, s.Closed })
            .ToListAsync();

        return Ok(sessions);
    }
}