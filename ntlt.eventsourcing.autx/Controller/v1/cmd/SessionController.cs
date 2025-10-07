using Microsoft.AspNetCore.Mvc;
using ntlt.eventsourcing.autx.SessionManagement;
using ntlt.eventsourcing.autx.SessionManagement.Cmd;

namespace ntlt.eventsourcing.autx.Controller.v1.cmd;

public class SessionController : V1CommandControllerBase
{
    private readonly SessionCommandService _sessionService;

    public SessionController(SessionCommandService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create()
    {
        var result = await _sessionService.CreateSession(new CreateSessionCmd());
        if (result.Success && result.ResultData != null) return Ok(new { SessionId = (Guid)result.ResultData });

        return StatusCode(500, new { result.ErrorMessage });
    }

    [HttpPost("end")]
    public async Task<IActionResult> End([FromBody] EndSessionCmd cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _sessionService.EndSession(cmd);

        if (result.Success) return Ok();

        return StatusCode(500, new { result.ErrorMessage });
    }
}