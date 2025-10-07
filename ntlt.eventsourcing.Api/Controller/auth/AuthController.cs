using ntlt.eventsourcing.Api.SessionManagement;
using ntlt.eventsourcing.Api.SessionManagement.Cmd;
using Microsoft.AspNetCore.Mvc;

namespace ntlt.eventsourcing.Api.Controller.auth;

/// <summary>
///     OAuth2-compatible authentication controller
/// </summary>
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SessionCommandService _sessionService;

    public AuthController(SessionCommandService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    ///     OAuth2 token endpoint - creates a new session and returns SessionId as access_token
    /// </summary>
    [HttpPost("api/auth/token")]
    public async Task<IActionResult> GetToken()
    {
        var result = await _sessionService.CreateSession(new CreateSessionCmd());

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