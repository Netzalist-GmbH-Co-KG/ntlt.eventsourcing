using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ntlt.eventsourcing.autx.Infrastructure.Middleware;
using ntlt.eventsourcing.autx.Infrastructure.ModelBinders;
using ntlt.eventsourcing.autx.SessionManagement;
using ntlt.eventsourcing.autx.UserManagement;


namespace ntlt.eventsourcing.autx;

public static class AutxInitialization
{
    public static void RegisterAutxMarten(this StoreOptions options)
    {
        options.Schema.For<User>()
            .Index(x => x.UserName, idx => idx.IsUnique = true)
            .Index(x => x.Email, idx => idx.IsUnique = true);

        options.Schema.For<Session>().Identity(x => x.SessionId);
        options.Schema.For<User>().Identity(x => x.UserId);

        options.Schema.For<SessionActivity>().Identity(x => x.SessionId);

        options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<SessionProjection>(ProjectionLifecycle.Inline);
    }

    public static void InjectModelBinder(this MvcOptions options)
    {
        // Register custom model binder for ICmd types
        // This automatically injects SessionId from HttpContext, eliminating Request DTOs
        options.ModelBinderProviders.Insert(0, new CmdModelBinderProvider());
    }
    public static void RegisterAutxServices(this IServiceCollection services)
    {
        services.AddScoped<UserCommandService>();
        services.AddScoped<SessionCommandService>();
    }

    public static void AddSessionValidationMiddleware(this WebApplication app)
    {
        app.UseMiddleware<SessionValidationMiddleware>();        
    }
}