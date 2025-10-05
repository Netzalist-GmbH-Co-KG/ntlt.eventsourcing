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
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Group by CQRS + Version
    options.SwaggerDoc("v1-commands", new OpenApiInfo
    {
        Title = "Marten Akka API - Commands (v1)",
        Version = "v1",
        Description = "CQRS Write Side - Command endpoints for modifying state"
    });

    options.SwaggerDoc("v1-queries", new OpenApiInfo
    {
        Title = "Marten Akka API - Queries (v1)",
        Version = "v1",
        Description = "CQRS Read Side - Query endpoints for retrieving data"
    });

    options.SwaggerDoc("infrastructure", new OpenApiInfo
    {
        Title = "Marten Akka API - Infrastructure",
        Version = "v1",
        Description = "Authentication and system management endpoints"
    });

    // Group endpoints by ApiExplorerSettings GroupName
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;

        var groupName = apiDesc.ActionDescriptor
            .EndpointMetadata
            .OfType<ApiExplorerSettingsAttribute>()
            .FirstOrDefault()?.GroupName;

        return docName == (groupName ?? "infrastructure");
    });

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

// Command Services (replacing Akka.NET actors)
builder.Services.AddScoped<UserCommandService>();
builder.Services.AddScoped<SessionCommandService>();
builder.Services.AddScoped<RebuildProjectionService>();

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

        options.Schema.For<SessionActivity>().Identity(x => x.SessionId);

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
        options.SwaggerEndpoint("/swagger/v1-commands/swagger.json", "Commands (v1)");
        options.SwaggerEndpoint("/swagger/v1-queries/swagger.json", "Queries (v1)");
        options.SwaggerEndpoint("/swagger/infrastructure/swagger.json", "Infrastructure");

        options.OAuthClientId("swagger-ui");
        options.OAuthAppName("Marten Akka API");
    });
}

app.Run();