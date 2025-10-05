using Akka.Hosting;
using JasperFx.Events.Projections;
using Marten;
using MartenAkkaTests.Api.Common;
using MartenAkkaTests.Api.EventSourcing;
using MartenAkkaTests.Api.Infrastructure.Middleware;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.SessionManagement.Cmd;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.Cmd;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // OAuth2 Password Flow for Swagger UI
    options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Password = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/api/auth/token", UriKind.Relative),
                Scopes = new Dictionary<string, string>()
            }
        },
        Description = "Click 'Authorize' to create a new session. Leave username/password empty."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "OAuth2"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
        
        var userManagementCmdRouter = system.ActorOf(UserManagementCmdRouter.Prop(sp), "UserManagementCmdRouter");
        registry.Register<UserManagementCmdRouter>(userManagementCmdRouter);

        var sessionManagementCmdRouter = system.ActorOf(SessionManagementCmdRouter.Prop(sp), "SessionManagementCmdRouter");
        registry.Register<SessionManagementCmdRouter>(sessionManagementCmdRouter);
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// IMPORTANT: Middleware must be registered BEFORE MapControllers
app.UseMiddleware<SessionValidationMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("swagger-ui");
        options.OAuthAppName("Marten Akka API");
    });
}

app.Run();