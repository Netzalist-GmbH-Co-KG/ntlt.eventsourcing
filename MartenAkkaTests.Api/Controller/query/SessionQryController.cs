using Marten;
using MartenAkkaTests.Api.SessionManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.query;

public class SessionQryController : ControllerBase
{
    private readonly IDocumentStore _documentStore;

    public SessionQryController(
        IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    [HttpGet("api/query/session/list")]
    public async Task<IActionResult> GetAllSessions([FromQuery] Guid sessionId)
    {
        await using var session = _documentStore.LightweightSession();
        var sessionExists = session.Query<Session>()
            .Any(u => u.SessionId == sessionId && u.Closed == false);
        if (!sessionExists)
        {
            return new UnauthorizedResult();
        }
        
        var sessions = await session.Query<Session>()
            .Select(u => new { u.SessionId, u.CreatedAt, u.LastAccessedAt, u.Closed })
            .ToListAsync();
        
        return new OkObjectResult(sessions);
    }

}