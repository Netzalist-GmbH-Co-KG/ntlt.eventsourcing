using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.cmd;

public class UserController : V1CommandControllerBase
{
    private readonly UserCommandService _userService;

    public UserController(UserCommandService userService)
    {
        _userService = userService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserCmd cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _userService.CreateUser(cmd);

        if (result.Success && result.ResultData != null) return Ok(new { UserId = (Guid)result.ResultData });

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("add-password-authentication")]
    public async Task<IActionResult> AddPasswordAuthentication([FromBody] AddPasswordAuthenticationCmd cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _userService.AddPasswordAuthentication(cmd);

        if (result.Success) return Ok();

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate([FromBody] DeactivateUserCmd cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _userService.DeactivateUser(cmd);

        if (result.Success) return Ok();

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeUserEmailCmd cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _userService.ChangeUserEmail(cmd);

        if (result.Success) return Ok();

        return StatusCode(500, new { result.ErrorMessage });
    }
}