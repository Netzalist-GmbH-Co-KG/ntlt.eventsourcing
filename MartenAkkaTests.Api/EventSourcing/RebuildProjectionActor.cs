using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;

namespace MartenAkkaTests.Api.EventSourcing;

// Cmd
public sealed record RebuildProjectionCommand(Guid? SessionId, string? ProjectionName = null) : ICmd;

// Cmd Handler
public class RebuildProjectionActor : CmdHandlerBase
{
    public RebuildProjectionActor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ReceiveAsync<RebuildProjectionCommand>(async cmd => await HandleCmdInSession(HandleRebuild, cmd));
    }

    private async Task<CommandResult> HandleRebuild(RebuildProjectionCommand cmd, IDocumentSession documentSession, Session session)
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
    }

    public static Props Prop(IServiceProvider serviceProvider)
    {
        return Props.Create(() => new RebuildProjectionActor(serviceProvider));
    }

}