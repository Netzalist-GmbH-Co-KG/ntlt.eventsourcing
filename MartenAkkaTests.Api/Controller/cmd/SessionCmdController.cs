using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.Controller.cmd.Requests;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.cmd;

public class SessionCmdController : ControllerBase
{
    private readonly IActorRef _sessionManagementCmdRouter;

    public SessionCmdController(IRequiredActor<SessionManagementCmdRouter> sessionManagementCmdRouter)
    {
        _sessionManagementCmdRouter = sessionManagementCmdRouter.ActorRef;
    }

    [HttpPost("api/cmd/session/create")]
    public async Task<IActionResult> CreateSession()
    {
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(new CreateSessionCmd());
        if (result.Success && result.ResultData != null)
        {
            return Ok(new { SessionId = (Guid)result.ResultData });
        }

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("api/cmd/session/end")]
    public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(
            new EndSessionCmd(sessionId, request.Reason));

        if (result.Success)
        {
            return Ok();
        }

        return StatusCode(500, new { result.ErrorMessage });
    }
}