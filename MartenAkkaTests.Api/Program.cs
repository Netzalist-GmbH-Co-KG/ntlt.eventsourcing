using Akka.Hosting;
using JasperFx.Events.Projections;
using Marten;
using MartenAkkaTests.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var connectionString = "host=localhost:5435;database=eventsourcing;username=postgres;password=postgres";
builder.Services.AddMarten(options =>
    {
        // Establish the connection string to your Marten database
        options.Connection(connectionString);
        options.Projections.Add<SomethingCounterProjection>(ProjectionLifecycle.Inline);
    })
    .UseLightweightSessions();

builder.Services.AddAkka("akka-universe", (builder, sp) =>
{
    builder.WithActors((system, registry) =>
    {
        var somethingActor = system.ActorOf(SomethingActor.Prop(sp), "somethingActor");
        registry.Register<SomethingActor>(somethingActor);

        var rebuildActor = system.ActorOf(RebuildProjectionActor.Prop(sp), "rebuildProjectionActor");
        registry.Register<RebuildProjectionActor>(rebuildActor);
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();

app.UseHttpsRedirection();

app.Run();