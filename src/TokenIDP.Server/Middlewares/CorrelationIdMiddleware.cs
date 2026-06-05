using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TokenIDP.Server.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

        var correlationIdValue = correlationId.ToString().Trim('"');
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationIdValue
        }))
        {
            // Store in HttpContext for later use
            context.Items["CorrelationId"] = correlationIdValue;

            // Continue to the next middleware
            await _next(context);
        }
    }
}

