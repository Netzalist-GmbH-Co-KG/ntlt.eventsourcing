using Marten;
using MartenAkkaTests.Api.SessionManagement;

namespace MartenAkkaTests.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware that validates SessionId from Bearer token and injects it into HttpContext
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> UnauthenticatedPaths = new()
    {
        "/swagger",
        "/api/v1/cmd/session/create",
        "/api/auth/token"
    };

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
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid Authorization header. Use 'Bearer {sessionId}'" });
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
            .AnyAsync();

        if (!validSession)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or closed session" });
            return;
        }

        // Store SessionId in HttpContext for controllers
        context.Items["SessionId"] = sessionId;

        await _next(context);
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        return UnauthenticatedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
