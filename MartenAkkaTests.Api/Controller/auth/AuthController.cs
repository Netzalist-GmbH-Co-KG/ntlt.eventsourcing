using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.auth;

/// <summary>
/// OAuth2-compatible authentication controller
/// </summary>
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IActorRef _sessionManagementCmdRouter;

    public AuthController(IRequiredActor<SessionManagementCmdRouter> sessionManagementCmdRouter)
    {
        _sessionManagementCmdRouter = sessionManagementCmdRouter.ActorRef;
    }

    /// <summary>
    /// OAuth2 token endpoint - creates a new session and returns SessionId as access_token
    /// </summary>
    [HttpPost("api/auth/token")]
    public async Task<IActionResult> GetToken()
    {
        var result = await _sessionManagementCmdRouter.Ask<CommandResult>(new CreateSessionCmd());

        if (result.Success && result.ResultData != null)
        {
            var sessionId = (Guid)result.ResultData;

            // OAuth2-compatible response format
            return Ok(new
            {
                access_token = sessionId.ToString(),
                token_type = "Bearer",
                expires_in = 3600 // 1 hour (informational, not enforced yet)
            });
        }

        return StatusCode(500, new { error = "Failed to create session", details = result.ErrorMessage });
    }
}
