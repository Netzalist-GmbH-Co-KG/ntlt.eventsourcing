namespace MartenAkkaTests.Api.Infrastructure.Extensions;

/// <summary>
/// Extension methods for HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the authenticated SessionId from HttpContext.
    /// Must be called after SessionValidationMiddleware has run.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if SessionId is not found in context</exception>
    public static Guid GetSessionId(this HttpContext context)
    {
        if (context.Items.TryGetValue("SessionId", out var sessionId) && sessionId is Guid guid)
        {
            return guid;
        }

        throw new InvalidOperationException(
            "SessionId not found in HttpContext. " +
            "Ensure SessionValidationMiddleware is registered and the endpoint is not marked [AllowAnonymous].");
    }
}
