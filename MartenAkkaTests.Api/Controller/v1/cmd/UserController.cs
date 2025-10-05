using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.Controller.v1.cmd.Requests;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.cmd;

public class UserController : V1CommandControllerBase
{
    private readonly IActorRef _userManagementCmdRouter;

    public UserController(IRequiredActor<UserManagementCmdRouter> userManagementCmdRouter)
    {
        _userManagementCmdRouter = userManagementCmdRouter.ActorRef;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _userManagementCmdRouter.Ask<CommandResult>(
            new CreateUserCmd(sessionId, request.UserName, request.Email));

        if (result.Success && result.ResultData != null)
        {
            return Ok(new { UserId = (Guid)result.ResultData });
        }

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("add-password-authentication")]
    public async Task<IActionResult> AddPasswordAuthentication([FromBody] AddPasswordAuthenticationRequest request)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _userManagementCmdRouter.Ask<CommandResult>(
            new AddPasswordAuthenticationCmd(sessionId, request.UserId, request.Password));

        if (result.Success)
        {
            return Ok();
        }

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateUserRequest request)
    {
        var sessionId = HttpContext.GetSessionId();
        var result = await _userManagementCmdRouter.Ask<CommandResult>(
            new DeactivateUserCmd(sessionId, request.UserId));

        if (result.Success)
        {
            return Ok();
        }

        return StatusCode(500, new { result.ErrorMessage });
    }
}
