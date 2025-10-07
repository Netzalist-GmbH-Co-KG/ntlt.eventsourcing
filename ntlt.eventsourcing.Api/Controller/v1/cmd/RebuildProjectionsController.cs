using ntlt.eventsourcing.Api.EventSourcing;
using Microsoft.AspNetCore.Mvc;

namespace ntlt.eventsourcing.Api.Controller.v1.cmd;

public class RebuildProjectionsController : V1CommandControllerBase
{
    private readonly RebuildProjectionService _rebuildService;

    public RebuildProjectionsController(RebuildProjectionService rebuildService)
    {
        _rebuildService = rebuildService;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RebuildProjections([FromBody] RebuildProjectionCommand cmd)
    {
        // SessionId is automatically injected by CmdModelBinder
        var result = await _rebuildService.RebuildProjections(cmd);

        return result.Success
            ? Ok(new
            {
                message = cmd.ProjectionName != null
                    ? $"Projection '{cmd.ProjectionName}' rebuilt successfully"
                    : "All projections rebuilt successfully",
                projections = result.ResultData
            })
            : StatusCode(500, $"Rebuild failed: {result.ErrorMessage}");
    }
}