using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using TokenIDP.Core.Abstractions.Telemetry;
using TokenIDP.Server.Telemetry;

namespace TokenIDP.Server.Middlewares;

internal sealed class RequestLatencyTelemetryMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLatencyTelemetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestLatencyTelemetryStore requestLatencyTelemetryStore,
        ClientTenantResolver clientTenantResolver)
    {
        if (!ShouldTrack(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var startedAtUtc = DateTime.UtcNow;
        var start = Stopwatch.GetTimestamp();
        var failed = false;

        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var clientId = await ResolveClientIdAsync(context);
            var tenantId = ResolveTenantId(context.User);

            if ((!tenantId.HasValue || tenantId.Value <= 0) && !string.IsNullOrWhiteSpace(clientId))
            {
                tenantId = await clientTenantResolver.ResolveTenantIdAsync(
                    clientId,
                    context.RequestAborted);
            }

            var measurement = new RequestLatencyMeasurement
            {
                TimestampUtc = startedAtUtc,
                DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                TenantId = tenantId,
                ClientId = clientId ?? string.Empty,
                Route = ResolveRoute(context),
                Method = context.Request.Method,
                StatusCode = failed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode
            };

            requestLatencyTelemetryStore.Record(measurement);
            RequestLatencyMetrics.Record(measurement);
        }
    }

    private static bool ShouldTrack(PathString path)
    {
        var value = path.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/connect", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/.well-known", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ResolveTenantId(ClaimsPrincipal? user)
    {
        var value = user?.FindFirstValue("uid");
        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }

    private static string ResolveRoute(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint routeEndpoint &&
            !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return routeEndpoint.RoutePattern.RawText;
        }

        return context.Request.Path.Value ?? "/";
    }

    private static async Task<string?> ResolveClientIdAsync(HttpContext context)
    {
        var fromClaims = context.User.FindFirstValue("client_id");
        if (!string.IsNullOrWhiteSpace(fromClaims))
        {
            return fromClaims.Trim();
        }

        var fromBasicAuth = ResolveClientIdFromBasicAuth(context.Request);
        if (!string.IsNullOrWhiteSpace(fromBasicAuth))
        {
            return fromBasicAuth;
        }

        var fromQuery = context.Request.Query["client_id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromQuery))
        {
            return fromQuery.Trim();
        }

        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            context.Request.HasFormContentType)
        {
            context.Request.EnableBuffering();
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            return form["client_id"].FirstOrDefault()?.Trim();
        }

        return null;
    }

    private static string? ResolveClientIdFromBasicAuth(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var encoded = authorization["Basic ".Length..].Trim();
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separatorIndex = raw.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return null;
            }

            var clientId = raw[..separatorIndex].Trim();
            return string.IsNullOrWhiteSpace(clientId) ? null : clientId;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
