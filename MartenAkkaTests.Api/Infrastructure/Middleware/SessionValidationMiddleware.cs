using Marten;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.Infrastructure.Middleware;

/// <summary>
///     Middleware that validates SessionId from Bearer token and injects it into HttpContext
/// </summary>
public class SessionValidationMiddleware
{
    private static readonly HashSet<string> UnauthenticatedPaths = new()
    {
        "/swagger",
        "/api/v1/cmd/session/create",
        "/api/auth/token"
    };

    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDocumentStore documentStore)
    {
        // Skip authentication for session creation and token endpoints
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract Bearer Token
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
                { error = "Missing or invalid Authorization header. Use 'Bearer {sessionId}'" });
            return;
        }

        var sessionIdStr = authHeader.Substring("Bearer ".Length).Trim();
        if (!Guid.TryParse(sessionIdStr, out var sessionId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid session ID format. Expected GUID." });
            return;
        }

        // Validate Session exists and is not closed
        await using var session = documentStore.LightweightSession();
        var validSession = await session.Query<Session>()
            .Where(s => s.SessionId == sessionId && !s.Closed)
            .FirstOrDefaultAsync();

        if (validSession == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or closed session" });
            return;
        }

        // Store both SessionId AND Session object in HttpContext for controllers
        // This avoids duplicate DB queries in command handlers
        context.Items["SessionId"] = sessionId;
        context.Items["Session"] = validSession;

        // Track session activity in CRUD table (not event sourced)
        var activity = new SessionActivity(sessionId, DateTime.UtcNow);
        session.Store(activity);
        await session.SaveChangesAsync();

        await _next(context);
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        return UnauthenticatedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}