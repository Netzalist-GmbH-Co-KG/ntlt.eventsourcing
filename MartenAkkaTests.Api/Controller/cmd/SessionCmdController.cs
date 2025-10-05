using System.Net;
using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.cmd;

public class SessionCmdController : ControllerBase
{
    private readonly IActorRef _sessionManagementCmdRouter;

    public SessionCmdController(
        IRequiredActor<SessionManagementCmdRouter> sessionManagementCmdRouter)
    {
        _sessionManagementCmdRouter = sessionManagementCmdRouter.ActorRef;
    }

    [HttpPost("api/cmd/session/create")]
    public async Task<IActionResult> CreateSession()
    {
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(new CreateSessionCmd());
        if (result.Success && result.ResultData!=null)
        {
            return new OkObjectResult(new { SessionId = (Guid) result.ResultData });
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    [HttpPost("api/cmd/session/end")]
    public async Task<IActionResult> EndSession([FromQuery] Guid sessionId,[FromQuery] string reason = "UserRequest")
    {
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(new EndSessionCmd(sessionId, reason));
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
}