using System.Net;
using Akka.Actor;
using Akka.Hosting;
using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.CreateSession;
using MartenAkkaTests.Api.SessionManagement.EndSession;
using MartenAkkaTests.Api.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller;

public class SessionController
{
    private readonly IDocumentStore _documentStore;
    private readonly IActorRef _createSessionActor;
    private readonly IActorRef _endSessionActor;

    public SessionController(
        IRequiredActor<CreateSessionCmdHandler> createSessionActor,
        IRequiredActor<EndSessionCmdHandler> endSessionActor,
        IDocumentStore documentStore)
    {
        _documentStore = documentStore;
        _createSessionActor = createSessionActor.ActorRef;
        _endSessionActor = endSessionActor.ActorRef;
    }

    [HttpPost("api/session/create")]
    public async Task<IActionResult> CreateSession()
    {
        var result = await _createSessionActor.Ask<CreateSessionResult>(new CreateSessionCmd());
        if (result.Success && result.SessionId.HasValue)
        {
            return new OkObjectResult(new { result.SessionId });
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    [HttpPost("api/session/end")]
    public async Task<IActionResult> EndSession([FromQuery] Guid sessionId,[FromQuery] string reason = "UserRequest")
    {
        var result = await _endSessionActor.Ask<EndSessionResult>(new EndSessionCmd(sessionId, reason));
        if (result.Success)
        {
            return new OkResult();
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    [HttpGet("api/session/list")]
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