using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;

namespace MartenAkkaTests.Api.EventSourcing;

/// <summary>
/// Service for rebuilding projections.
/// Replaces RebuildProjectionActor.
/// </summary>
public class RebuildProjectionService : CommandServiceBase
{
    public RebuildProjectionService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<CommandResult> RebuildProjections(RebuildProjectionCommand cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, documentSession, session) =>
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
                // Rebuild ALL projections - manually list them or use reflection
                // For now, rebuild known projections explicitly
                var projectionTypes = new[] { typeof(Session), typeof(User) };

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
