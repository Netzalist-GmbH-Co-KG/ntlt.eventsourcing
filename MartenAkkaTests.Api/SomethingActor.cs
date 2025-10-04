using Akka.Actor;
using Marten;
using MartenAkkaTests.Api.Controller;

namespace MartenAkkaTests.Api;

public sealed record HandledOkNotification(Guid Id, int NewCounter);

public class SomethingActor : ReceiveActor
{
    private readonly IServiceProvider _serviceProvider;

    public SomethingActor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ReceiveAsync<SomethingHappenedCommand>(async cmd => await HandleMessage(cmd));
    }

    private async Task HandleMessage(SomethingHappenedCommand cmd)
    {
        //Create a new scope to get a new session instance
        using var scope = _serviceProvider.CreateScope();
        var session = scope.ServiceProvider.GetService<IDocumentSession>();

        if (session == null)
            throw new ArgumentNullException(nameof(session), "IDocumentSession cannot be null");

        session.Events.Append(cmd.Id, new SomethingHappened(cmd.Id, DateTime.Now.ToShortTimeString()));
        await session.SaveChangesAsync();

        Console.WriteLine($"Handled message: {cmd}");
        var counter = await session.LoadAsync<SomethingCounter>(cmd.Id);
        if (counter == null)
            throw new InvalidOperationException("Counter should not be null here");

        Sender.Tell(new HandledOkNotification(counter.Id, counter.Count));
    }

    public static Props Prop(IServiceProvider serviceProvider)
    {
        return Props.Create(() => new SomethingActor(serviceProvider));
    }

    public sealed record SomethingHappenedCommand(Guid Id);
}