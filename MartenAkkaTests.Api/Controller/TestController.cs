using Akka.Actor;
using Akka.Hosting;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller;

public sealed record SomethingHappened(Guid Id, string Name);

public class TestController : ControllerBase
{
    private readonly IActorRef _somethingActor;
    private readonly IActorRef _rebuildActor;
    private readonly IDocumentStore _store;

    public TestController(
        IRequiredActor<SomethingActor> somethingActor,
        IRequiredActor<RebuildProjectionActor> rebuildActor,
        IDocumentStore store )
    {
        _somethingActor = somethingActor.ActorRef;
        _rebuildActor = rebuildActor.ActorRef;
        _store = store;
    }
    [HttpGet("/test")]
    public async Task<IActionResult> Get()
    {
        try
        {
            var x = new Guid("00000000-0000-0000-0000-000000000001");
            var reply = await _somethingActor.Ask<HandledOkNotification>(new SomethingActor.SomethingHappenedCommand(x));
            return Ok("Accepted: " + reply.NewCounter);
        }
        catch (Exception e)
        {
            throw new ApplicationException(e.Message, e);
        }
    }
    
    [HttpGet("/test2")]
    public async Task<IActionResult> GetTest2()
    {
        await using var session = _store.LightweightSession();
        var counter = await session.LoadAsync<SomethingCounter>(new Guid("00000000-0000-0000-0000-000000000001"));
        return Ok("Counter: " + (counter?.Count ?? 0));
    }

    [HttpGet("/rebuild")]
    public async Task<IActionResult> RebuildProjections([FromQuery] string? projection = null)
    {
        try
        {
            var result = await _rebuildActor.Ask<object>(
                new RebuildProjectionActor.RebuildProjectionCommand(projection),
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