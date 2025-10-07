using System.Text.Json;
using ntlt.eventsourcing.Api.EventSourcing;
using ntlt.eventsourcing.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ntlt.eventsourcing.Api.Infrastructure.ModelBinders;

/// <summary>
///     Custom model binder for ICmd types.
///     Automatically injects SessionId from HttpContext into commands.
///     This eliminates the need for separate Request DTOs.
/// </summary>
public class CmdModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        // Only bind ICmd types
        if (!typeof(ICmd).IsAssignableFrom(bindingContext.ModelType))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        try
        {
            // Read JSON from request body
            var httpContext = bindingContext.HttpContext;
            httpContext.Request.EnableBuffering(); // Allow re-reading body

            using var reader = new StreamReader(httpContext.Request.Body);
            var json = await reader.ReadToEndAsync();
            httpContext.Request.Body.Position = 0; // Reset for potential re-reads

            if (string.IsNullOrEmpty(json))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            // Deserialize command from JSON (without SessionId in request)
            var cmd = JsonSerializer.Deserialize(json, bindingContext.ModelType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (cmd == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            // Inject SessionId from HttpContext (set by SessionValidationMiddleware)
            // Use reflection to set SessionId property
            var sessionIdProperty = bindingContext.ModelType.GetProperty("SessionId");
            if (sessionIdProperty != null && sessionIdProperty.CanWrite)
                try
                {
                    var sessionId = httpContext.GetSessionId();
                    sessionIdProperty.SetValue(cmd, sessionId);
                }
                catch (InvalidOperationException)
                {
                    // SessionId not in context - command might not require authentication
                    // (e.g., CreateSessionCmd). Leave SessionId as null.
                }

            bindingContext.Result = ModelBindingResult.Success(cmd);
        }
        catch (JsonException)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid JSON format");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Error binding model: {ex.Message}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}

/// <summary>
///     ModelBinderProvider for CmdModelBinder.
///     Registers the binder for all ICmd types.
/// </summary>
public class CmdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (typeof(ICmd).IsAssignableFrom(context.Metadata.ModelType)) return new CmdModelBinder();

        return null;
    }
}