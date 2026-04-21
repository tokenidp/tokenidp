using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Extensions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Infrastructure;
using TokenIDP.Server.Multitenancy;

namespace TokenIDP.Server.Middlewares;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolutionOptions _options;
    private readonly ICache _cache;
    private readonly IAppLogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptions<TenantResolutionOptions> options,
        ICache cache,
        IAppLogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantResolver tenantResolver,
        ITenantContextAccessor tenantContextAccessor)
    {
        if (ShouldSkipResolution(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!TenantHostParser.TryResolveTenantKey(context.Request.Host.Host, _options, out var tenantKey))
        {
            await RejectUnavailableTenantAsync(context, "invalid_host");
            return;
        }

        context.Items[HostTenantResolver.TenantKeyItemName] = tenantKey;

        var tenantContext = await tenantResolver.ResolveAsync(context, context.RequestAborted);
        if (tenantContext is null)
        {
            await RejectUnavailableTenantAsync(context, "tenant_unavailable");
            return;
        }

        tenantContextAccessor.SetTenant(tenantContext);

        try
        {
            await _next(context);
        }
        finally
        {
            tenantContextAccessor.Clear();
            context.Items.Remove(HostTenantResolver.TenantKeyItemName);
        }
    }

    private static bool ShouldSkipResolution(PathString path)
    {
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }

    private async Task RejectUnavailableTenantAsync(HttpContext context, string reason)
    {
        var throttleCount = await IncrementInvalidAttemptCountAsync(context);
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogWarning(
            "Tenant host rejected. Host={Host}, Path={Path}, Reason={Reason}, Count={Count}, RemoteIp={RemoteIp}",
            context.Request.Host.Value,
            context.Request.Path.Value ?? string.Empty,
            reason,
            throttleCount,
            remoteIp);

        context.Response.StatusCode = throttleCount >= _options.InvalidHostThrottleMaxAttempts
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(new
        {
            error = context.Response.StatusCode == StatusCodes.Status429TooManyRequests
                ? "too_many_requests"
                : "tenant_unavailable"
        });
    }

    private async Task<int> IncrementInvalidAttemptCountAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = CacheKeys.TENANT.FormatCacheKey("invalid-host", remoteIp, context.Request.Host.Host);
        var attempts = await _cache.GetAsync<int>(cacheKey);
        var nextAttempt = attempts + 1;

        await _cache.SetAsync(
            cacheKey,
            nextAttempt,
            TimeSpan.FromSeconds(Math.Max(1, _options.InvalidHostThrottleWindowSeconds)));

        return nextAttempt;
    }
}
