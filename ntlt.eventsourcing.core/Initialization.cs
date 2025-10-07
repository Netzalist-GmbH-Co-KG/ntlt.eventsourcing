using Marten;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using ntlt.eventsourcing.core.Common;
using ntlt.eventsourcing.core.EventSourcing;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ntlt.eventsourcing.core;

public static class Initialization
{
    public static void AddNtltSerilog(this ConfigureHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
    }
    
    public static void InitNtltEventSourcing(this StoreOptions options, string connectionString)
    {
        // Establish the connection string to your Marten database
        options.Connection(connectionString);
    }

    public static void InitNtltEventSourcing(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IGuidProvider, GuidProvider>();  
        services.AddScoped<RebuildProjectionService>();
    }

    public static void InitNtltDocGroupings(this SwaggerGenOptions options, string appName)
    {
        // Group by CQRS + Version
        options.SwaggerDoc("v1-commands", new OpenApiInfo
        {
            Title = $"{appName} - Commands (v1)",
            Version = "v1",
            Description = "CQRS Write Side - Command endpoints for modifying state"
        });

        options.SwaggerDoc("v1-queries", new OpenApiInfo
        {
            Title = $"{appName} - Queries (v1)",
            Version = "v1",
            Description = "CQRS Read Side - Query endpoints for retrieving data"
        });

        options.SwaggerDoc("infrastructure", new OpenApiInfo
        {
            Title = $"{appName} - Infrastructure",
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

    }    
    public static void InitNtltSecurity(this SwaggerGenOptions options)
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
                []
            }
        });
    }

    public static void RegisterNtltSwaggerUi(this SwaggerUIOptions options, string appName)
    {
        options.SwaggerEndpoint("/swagger/v1-commands/swagger.json", "Commands (v1)");
        options.SwaggerEndpoint("/swagger/v1-queries/swagger.json", "Queries (v1)");
        options.SwaggerEndpoint("/swagger/infrastructure/swagger.json", "Infrastructure");

        options.OAuthClientId("swagger-ui");
        options.OAuthAppName(appName);
    }
}