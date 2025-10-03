using Akka.Actor;
using Marten;

namespace MartenAkkaTests.Api;

public class RebuildProjectionActor : ReceiveActor
{
    private readonly IServiceProvider _serviceProvider;

    public RebuildProjectionActor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ReceiveAsync<RebuildProjectionCommand>(async cmd => await HandleRebuild(cmd));
    }

    private async Task HandleRebuild(RebuildProjectionCommand cmd)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var daemon = await store.BuildProjectionDaemonAsync();
            var stats = new Dictionary<string, long>();

            if (!string.IsNullOrEmpty(cmd.ProjectionName))
            {
                // Rebuild specific projection
                await daemon.RebuildProjectionAsync(cmd.ProjectionName, CancellationToken.None);
                stats[cmd.ProjectionName] = 1;
                Console.WriteLine($"Projection '{cmd.ProjectionName}' rebuilt successfully");
            }
            else
            {
                // Rebuild ALL projections - manually list them or use reflection
                // For now, rebuild known projections explicitly
                var projectionTypes = new[] { typeof(SomethingCounter) };

                foreach (var projectionType in projectionTypes)
                {
                    var projectionName = projectionType.Name;
                    await daemon.RebuildProjectionAsync(projectionName, CancellationToken.None);
                    stats[projectionName] = 1;
                    Console.WriteLine($"Projection '{projectionName}' rebuilt");
                }

                Console.WriteLine($"All projections rebuilt successfully. Total: {stats.Count}");
            }

            Sender.Tell(new RebuildCompletedNotification(stats));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rebuild failed: {ex.Message}");
            Sender.Tell(new RebuildFailedNotification(ex.Message));
        }
    }

    public static Props Prop(IServiceProvider serviceProvider)
    {
        return Props.Create(() => new RebuildProjectionActor(serviceProvider));
    }

    public sealed record RebuildProjectionCommand(string? ProjectionName = null);

    public sealed record RebuildCompletedNotification(Dictionary<string, long> ProjectionStats);

    public sealed record RebuildFailedNotification(string Error);
}