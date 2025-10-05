using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.Controller.v1.cmd.Requests;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.cmd;

public class RebuildProjectionsController : V1CommandControllerBase
{
    private readonly IActorRef _rebuildActor;

    public RebuildProjectionsController(
        IRequiredActor<RebuildProjectionActor> rebuildActor)
    {
        _rebuildActor = rebuildActor.ActorRef;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RebuildProjections([FromBody] RebuildProjectionsRequest request)
    {
        try
        {
            var sessionId = HttpContext.GetSessionId();
            var result = await _rebuildActor.Ask<CommandResult>(
                new RebuildProjectionCommand(sessionId, request.Projection),
                TimeSpan.FromMinutes(5));

            return result.Success
                ? Ok(new
                {
                    message = request.Projection != null
                        ? $"Projection '{request.Projection}' rebuilt successfully"
                        : "All projections rebuilt successfully",
                    projections = result.ResultData
                })
                : StatusCode(500, $"Rebuild failed: {result.ErrorMessage}");
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Error triggering rebuild: {e.Message}");
        }
    }
}