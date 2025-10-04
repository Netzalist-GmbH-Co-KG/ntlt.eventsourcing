using Akka.Actor;
using Akka.Hosting;
using MartenAkkaTests.Api.EventSourcing;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller;

public class RebuildProjectionsController : ControllerBase
{
    private readonly IActorRef _rebuildActor;

    public RebuildProjectionsController(
        IRequiredActor<RebuildProjectionActor> rebuildActor)
    {
        _rebuildActor = rebuildActor.ActorRef;
    }

    [HttpGet("/rebuild")]
    public async Task<IActionResult> RebuildProjections([FromQuery] Guid sessionId, [FromQuery] string? projection = null)
    {
        try
        {
            var result = await _rebuildActor.Ask<object>(
                new RebuildProjectionActor.RebuildProjectionCommand(sessionId, projection),
                TimeSpan.FromMinutes(5));

            return result switch
            {
                RebuildProjectionActor.RebuildCompletedNotification completed =>
                    Ok(new
                    {
                        message = projection != null
                            ? $"Projection '{projection}' rebuilt successfully"
                            : "All projections rebuilt successfully",
                        projections = completed.ProjectionStats
                    }),
                RebuildProjectionActor.RebuildFailedNotification failed =>
                    StatusCode(500, $"Rebuild failed: {failed.Error}"),
                _ => StatusCode(500, "Unknown response from rebuild actor")
            };
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Error triggering rebuild: {e.Message}");
        }
    }
}