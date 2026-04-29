using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace TokenIDP.Core.OAuth.RateLimiting;

public sealed class OAuthRateLimitRejectionHandler
{
    private readonly IAppLogger<OAuthRateLimitRejectionHandler> _logger;

    public OAuthRateLimitRejectionHandler(IAppLogger<OAuthRateLimitRejectionHandler> logger)
    {
        _logger = logger;
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
        }
        else
        {
            response.Headers["X-RateLimit-Limit"] = OAuthClientRateLimitRequestContext.DefaultPermitLimit.ToString();
            response.Headers["X-RateLimit-Remaining"] = "0";

            _logger.LogWarning(
                "OAuth client rate limit exceeded without resolved metadata. Endpoint: {Endpoint}, TimestampUtc: {TimestampUtc}",
                httpContext.Request.Path.Value ?? string.Empty,
                DateTime.UtcNow);
        }

        var payload = new
        {
            error = "rate_limit_exceeded",
            error_description = "Too many requests for this client. Try again later."
        };

        await response.WriteAsync(JsonSerializer.Serialize(payload), cancellationToken);
    }
}
