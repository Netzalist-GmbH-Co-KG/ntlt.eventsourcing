using ntlt.eventsourcing.core.Common;

namespace ntlt.eventsourcing.core.EventSourcing;

/// <summary>
///     Service for rebuilding projections.
///     Replaces RebuildProjectionActor.
/// </summary>
public class RebuildProjectionService : CommandServiceBase
{
    public RebuildProjectionService(IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider, IGuidProvider guidProvider, ILogger<RebuildProjectionService> logger)
        : base(serviceProvider, dateTimeProvider, guidProvider, logger)
    {
    }

    public async Task<CommandResult> RebuildProjections(RebuildProjectionCommand cmd)
    {
        return await ExecuteCommand(cmd, async (cmd, documentSession) =>
        {
            var store = documentSession.DocumentStore;
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
                // Rebuild ALL projections - find all projection types via reflection
                var assembly = typeof(RebuildProjectionService).Assembly;
                var projectionTypes = assembly.GetTypes()
                    .Where(t => t.BaseType is { IsGenericType: true } &&
                                (t.BaseType.GetGenericTypeDefinition().Name == "SingleStreamProjection`2" ||
                                 t.BaseType.GetGenericTypeDefinition().Name == "MultiStreamProjection`2"))
                    .Select(t => t.BaseType!.GetGenericArguments()[0]) // Get TDoc type parameter
                    .Distinct()
                    .ToArray();

                foreach (var projectionType in projectionTypes)
                {
                    var projectionName = projectionType.Name;
                    await daemon.RebuildProjectionAsync(projectionName, CancellationToken.None);
                    stats[projectionName] = 1;
                    Console.WriteLine($"Projection '{projectionName}' rebuilt");
                }

                Console.WriteLine($"All projections rebuilt successfully. Total: {stats.Count}");
            }

            return new CommandResult(cmd, true, stats);
        });
    }
}