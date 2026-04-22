using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Foundation.Extensions;
using TokenIDP.Core.Foundation.Options;
using TokenIDP.Infrastructure;
using TokenIDP.Server.Multitenancy;
using Microsoft.Extensions.Hosting;

namespace TokenIDP.Server.Middlewares;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolutionOptions _options;
    private readonly ICache _cache;
    private readonly IAppLogger<TenantResolutionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptions<TenantResolutionOptions> options,
        ICache cache,
        IAppLogger<TenantResolutionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantRequestResolver tenantRequestResolver,
        ITenantResolver tenantResolver,
        ITenantContextAccessor tenantContextAccessor)
    {
        if (ShouldSkipResolution(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var requestResolution = tenantRequestResolver.Resolve(context);
        if (requestResolution.Status != TenantRequestResolutionStatus.Resolved ||
            string.IsNullOrWhiteSpace(requestResolution.TenantKey) ||
            requestResolution.Source is null)
        {
            await RejectUnavailableTenantAsync(
                context,
                requestResolution.FailureReason ?? "tenant_unavailable");
            return;
        }

        context.Items[TenantResolutionHttpContextItems.TenantKey] = requestResolution.TenantKey;
        context.Items[TenantResolutionHttpContextItems.ResolutionSource] = requestResolution.Source.Value.ToString();

        var tenantContext = await tenantResolver.ResolveAsync(requestResolution.TenantKey, context.RequestAborted);
        if (tenantContext is null)
        {
            await RejectUnavailableTenantAsync(context, "tenant_unavailable");
            return;
        }

        tenantContextAccessor.SetTenant(tenantContext);
        _logger.LogInfo(
            "Tenant resolved. ResolvedTenant={TenantKey}, ResolutionSource={ResolutionSource}, Environment={Environment}, Host={Host}, Path={Path}",
            tenantContext.TenantKey,
            requestResolution.Source.Value.ToString(),
            _environment.EnvironmentName,
            context.Request.Host.Value,
            context.Request.Path.Value ?? string.Empty);

        try
        {
            await _next(context);
        }
        finally
        {
            tenantContextAccessor.Clear();
            context.Items.Remove(TenantResolutionHttpContextItems.TenantKey);
            context.Items.Remove(TenantResolutionHttpContextItems.ResolutionSource);
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
            "Tenant resolution rejected. Host={Host}, Path={Path}, Reason={Reason}, Count={Count}, RemoteIp={RemoteIp}, Environment={Environment}",
            context.Request.Host.Value,
            context.Request.Path.Value ?? string.Empty,
            reason,
            throttleCount,
            remoteIp,
            _environment.EnvironmentName);

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
