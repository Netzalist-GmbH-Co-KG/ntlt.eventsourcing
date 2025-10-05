using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.Controller.v1.cmd.Requests;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.cmd;

public class SessionController : V1CommandControllerBase
{
    private readonly IActorRef _sessionManagementCmdRouter;

    public SessionController(IRequiredActor<SessionManagementCmdRouter> sessionManagementCmdRouter)
    {
        _sessionManagementCmdRouter = sessionManagementCmdRouter.ActorRef;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create()
    {
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(new CreateSessionCmd());
        if (result.Success && result.ResultData != null)
        {
            return Ok(new { SessionId = (Guid)result.ResultData });
        }

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("end")]
    public async Task<IActionResult> End([FromBody] EndSessionRequest request)
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
