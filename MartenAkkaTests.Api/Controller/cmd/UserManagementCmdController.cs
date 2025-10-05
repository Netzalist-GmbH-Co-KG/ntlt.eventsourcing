using System.Net;
using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.cmd;

public class UserManagementCmdController : ControllerBase
{
    private readonly IActorRef  _userManagementCmdRouter;

    public UserManagementCmdController(
        IRequiredActor<UserManagementCmdRouter> userManagementCmdRouter
        )
    {
        _userManagementCmdRouter = userManagementCmdRouter.ActorRef;
    }

    [HttpPost("api/cmd/user/create")]
    public async Task<IActionResult> CreateUser([FromQuery] Guid sessionId, [FromQuery] string userName, [FromQuery] string email)
    {
        var result = await _userManagementCmdRouter.Ask<CommandResult>(new CreateUserCmd(sessionId, userName, email));
        if (result.Success && result.ResultData != null)
        {
            return new OkObjectResult(new { UserId = (Guid) result.ResultData });
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    
    [HttpPost("api/cmd/user/add-password-authentication")]
    public async Task<IActionResult> AddPasswordAuthentication([FromQuery] Guid sessionId, [FromQuery] Guid userId, [FromQuery] string password)
    {
        var result = await _userManagementCmdRouter.Ask<CommandResult>(new AddPasswordAuthenticationCmd(sessionId, userId, password));
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
    
    [HttpPost("api/cmd/user/deactivate-user")]
    public async Task<IActionResult> DeactivateUser([FromQuery] Guid sessionId, [FromQuery] Guid userId)
    {
        var result = await _userManagementCmdRouter.Ask<CommandResult>( new DeactivateUserCmd(sessionId, userId));
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