using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.Controller.cmd.Requests;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.cmd;

public class UserManagementCmdController : ControllerBase
{
    private readonly IActorRef _userManagementCmdRouter;

    public UserManagementCmdController(IRequiredActor<UserManagementCmdRouter> userManagementCmdRouter)
    {
        _userManagementCmdRouter = userManagementCmdRouter.ActorRef;
    }

    [HttpPost("api/cmd/user/create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
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

    [HttpPost("api/cmd/user/add-password-authentication")]
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

    [HttpPost("api/cmd/user/deactivate-user")]
    public async Task<IActionResult> DeactivateUser([FromBody] DeactivateUserRequest request)
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