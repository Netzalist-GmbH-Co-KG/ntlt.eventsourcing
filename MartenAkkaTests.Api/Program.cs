using Akka.Hosting;
using JasperFx.Events.Projections;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.CreateSession;
using MartenAkkaTests.Api.SessionManagement.EndSession;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.AddAuthentication;
using MartenAkkaTests.Api.UserManagement.CreateUser;
using MartenAkkaTests.Api.UserManagement.DeactivateUser;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddTransient<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddTransient<IGuidProvider, GuidProvider>();

var connectionString = "host=localhost:5435;database=eventsourcing;username=postgres;password=postgres";
builder.Services.AddMarten(options =>
    {
        // Establish the connection string to your Marten database
        options.Connection(connectionString);
        options.Schema.For<User>()
            .Index(x => x.UserName, idx => idx.IsUnique = true)
            .Index(x => x.Email, idx => idx.IsUnique = true);
        
        options.Schema.For<Session>().Identity(x => x.SessionId);
        options.Schema.For<User>().Identity(x => x.UserId);    

        options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<SessionProjection>(ProjectionLifecycle.Inline);
    })
    .UseLightweightSessions();

builder.Services.AddAkka("akka-universe", (akkaConfigurationBuilder, sp) =>
{
    akkaConfigurationBuilder.WithActors((system, registry) =>
    {
        var rebuildActor = system.ActorOf(RebuildProjectionActor.Prop(sp), "rebuildProjectionActor");
        registry.Register<RebuildProjectionActor>(rebuildActor);
        
        var createUserCmdHandler = system.ActorOf(CreateUserCmdHandler.Prop(sp), "CreateUserCmdHandler");
        registry.Register<CreateUserCmdHandler>(createUserCmdHandler);

        var addPasswordAuthenticationCmdHandler = system.ActorOf(AddPasswordAuthenticationCmdHandler.Prop(sp), "AddPasswordAuthenticationCmdHandler");
        registry.Register<AddPasswordAuthenticationCmdHandler>(addPasswordAuthenticationCmdHandler);

        var deactivateUserCmdHandler = system.ActorOf(DeactivateUserCmdHandler.Prop(sp), "DeactivateUserCmdHandler");
        registry.Register<DeactivateUserCmdHandler>(deactivateUserCmdHandler);
        
        var createSessionCmdHandler = system.ActorOf(CreateSessionCmdHandler.Prop(sp), "CreateSessionCmdHandler");
        registry.Register<CreateSessionCmdHandler>(createSessionCmdHandler);
        
        var endSessionCmdHandler = system.ActorOf(EndSessionCmdHandler.Prop(sp), "EndSessionCmdHandler");
        registry.Register<EndSessionCmdHandler>(endSessionCmdHandler);

    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();