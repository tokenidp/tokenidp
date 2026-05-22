using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;
using TokenIDP.Domain.DomainEvents.Activities;
using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Core.OAuth.RateLimiting;

public sealed class OAuthRateLimitRejectionHandler
{
    private readonly IAppLogger<OAuthRateLimitRejectionHandler> _logger;
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;

    public OAuthRateLimitRejectionHandler(
        IAppLogger<OAuthRateLimitRejectionHandler> logger,
        IApplicationEventDispatcher applicationEventDispatcher)
    {
        _logger = logger;
        _applicationEventDispatcher = applicationEventDispatcher;
    }

    public async Task HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/json";

        var metadata = httpContext.Items[OAuthClientRateLimiter.ResponseMetadataKey]
            as OAuthClientRateLimitResponseMetadata;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        else if (metadata != null)
        {
            response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(metadata.TimeWindow.TotalSeconds)).ToString();
        }

        if (metadata != null)
        {
            response.Headers["X-RateLimit-Limit"] = metadata.PermitLimit.ToString();
            response.Headers["X-RateLimit-Remaining"] = Math.Max(0, metadata.RemainingPermits).ToString();

            _logger.LogWarning(
                "OAuth client rate limit exceeded. ClientId: {ClientId}, TenantId: {TenantId}, IP: {IP}, Endpoint: {Endpoint}, TimestampUtc: {TimestampUtc}, PermitLimit: {PermitLimit}, TimeWindowSeconds: {TimeWindowSeconds}",
                metadata.ClientId ?? "ip-fallback",
                metadata.TenantId.HasValue ? metadata.TenantId.Value : 0,
                metadata.IpAddress,
                metadata.Endpoint,
                DateTime.UtcNow,
                metadata.PermitLimit,
                metadata.TimeWindow.TotalSeconds);

            await RaiseRateLimitTriggeredAsync(httpContext, metadata, cancellationToken);
        }
        else
        {
            response.Headers["X-RateLimit-Limit"] = OAuthClientRateLimitRequestContext.DefaultPermitLimit.ToString();
            response.Headers["X-RateLimit-Remaining"] = "0";

            _logger.LogWarning(
                "OAuth client rate limit exceeded without resolved metadata. Endpoint: {Endpoint}, TimestampUtc: {TimestampUtc}",
                httpContext.Request.Path.Value ?? string.Empty,
                DateTime.UtcNow);

            await RaiseRateLimitTriggeredAsync(httpContext, null, cancellationToken);
        }

        var payload = new
        {
            error = "rate_limit_exceeded",
            error_description = "Too many requests for this client. Try again later."
        };

        await response.WriteAsync(JsonSerializer.Serialize(payload), cancellationToken);
    }

    private Task RaiseRateLimitTriggeredAsync(
        HttpContext httpContext,
        OAuthClientRateLimitResponseMetadata? metadata,
        CancellationToken cancellationToken)
    {
        var targetId = metadata?.ClientId ?? metadata?.IpAddress ?? ResolveIpAddress(httpContext);
        var targetDescription = metadata?.ClientId is not null
            ? $"Client {metadata.ClientId}"
            : $"IP {targetId}";
        var endpoint = metadata?.Endpoint ?? httpContext.Request.Path.Value ?? string.Empty;

        return _applicationEventDispatcher.RaiseAsync(
            new ActivityDomainEvent(
                TenantId: metadata?.TenantId ?? 0,
                EventType: ActivityEventType.RateLimitTriggered,
                AggregateType: "RateLimit",
                AggregateId: targetId,
                ActorId: null,
                ActorDisplayName: null,
                TargetId: targetId,
                TargetDescription: targetDescription,
                Status: "Rejected",
                Description: $"Rate limit triggered for {targetDescription} on {endpoint}.",
                CorrelationId: ResolveCorrelationId(httpContext),
                IpAddress: ResolveIpAddress(httpContext),
                UserAgent: httpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);
    }

    private static string ResolveIpAddress(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    private static Guid? ResolveCorrelationId(HttpContext httpContext)
    {
        var value = httpContext.Items["CorrelationId"]?.ToString();
        return Guid.TryParse(value, out var correlationId)
            ? correlationId
            : null;
    }
}
