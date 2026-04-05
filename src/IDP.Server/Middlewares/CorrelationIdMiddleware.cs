using Microsoft.AspNetCore.Http;
using NLog;

namespace IDP.Server.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the correlation ID exists in the incoming request header
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            // Generate a new correlation ID if not present in request header
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Append(CorrelationIdHeader, correlationId);
        }

        // Add the correlation ID to the response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Add the correlation ID to the NLog logging context
        using (ScopeContext.PushProperty("CorrelationId", correlationId.ToString().Trim('"')))
        {
            // Store in HttpContext for later use
            context.Items["CorrelationId"] = correlationId;

            // Continue to the next middleware
            await _next(context);
        }
    }
}
