using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace TokenIDP.Core.OAuth.RateLimiting;

public sealed class OAuthClientRateLimiter : PartitionedRateLimiter<HttpContext>
{
    internal const string ResponseMetadataKey = "__oauth_rate_limit_response_metadata";

    private readonly ConcurrentDictionary<string, PartitionLimiterState> _limiters =
        new(StringComparer.Ordinal);

    public override RateLimiterStatistics? GetStatistics(HttpContext resource)
    {
        var resolvedContext = ResolveContext(resource);
        if (!resolvedContext.ShouldRateLimit)
        {
            return null;
        }

        return TryGetLimiterState(resolvedContext.PartitionKey, out var state)
            ? state.Limiter.GetStatistics()
            : null;
    }

    protected override RateLimitLease AttemptAcquireCore(HttpContext resource, int permitCount)
    {
        var resolvedContext = ResolveContext(resource);
        if (!resolvedContext.ShouldRateLimit)
        {
            return SuccessfulRateLimitLease.Instance;
        }

        var limiter = GetOrCreateLimiter(resolvedContext);
        var lease = limiter.AttemptAcquire(permitCount);

        StoreResponseMetadata(resource, resolvedContext, limiter);

        return lease;
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        HttpContext resource,
        int permitCount,
        CancellationToken cancellationToken)
    {
        var resolvedContext = await ResolveContextAsync(resource, cancellationToken);
        if (!resolvedContext.ShouldRateLimit)
        {
            return SuccessfulRateLimitLease.Instance;
        }

        var limiter = GetOrCreateLimiter(resolvedContext);
        var lease = await limiter.AcquireAsync(permitCount, cancellationToken);

        StoreResponseMetadata(resource, resolvedContext, limiter);

        return lease;
    }

    protected override ValueTask DisposeAsyncCore()
    {
        foreach (var limiter in _limiters.Values)
        {
            limiter.Limiter.Dispose();
        }

        _limiters.Clear();
        return ValueTask.CompletedTask;
    }

    private OAuthClientRateLimitRequestContext ResolveContext(HttpContext resource)
    {
        return ResolveContextAsync(resource, CancellationToken.None).GetAwaiter().GetResult();
    }

    private async ValueTask<OAuthClientRateLimitRequestContext> ResolveContextAsync(
        HttpContext resource,
        CancellationToken cancellationToken)
    {
        var resolver = resource.RequestServices.GetRequiredService<IOAuthClientRateLimitRequestResolver>();
        return await resolver.ResolveAsync(resource, cancellationToken);
    }

    private FixedWindowRateLimiter GetOrCreateLimiter(OAuthClientRateLimitRequestContext context)
    {
        var desiredConfiguration = new PartitionLimiterConfiguration(
            context.PermitLimit,
            context.QueueLimit,
            context.TimeWindow);

        var state = _limiters.AddOrUpdate(
            context.PartitionKey,
            _ => CreateLimiterState(desiredConfiguration),
            (_, current) =>
            {
                if (current.Configuration == desiredConfiguration)
                {
                    return current;
                }

                current.Limiter.Dispose();
                return CreateLimiterState(desiredConfiguration);
            });

        return state.Limiter;
    }

    private static PartitionLimiterState CreateLimiterState(PartitionLimiterConfiguration configuration)
    {
        var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = configuration.PermitLimit,
            QueueLimit = configuration.QueueLimit,
            Window = configuration.TimeWindow,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

        return new PartitionLimiterState(configuration, limiter);
    }

    private bool TryGetLimiterState(string partitionKey, out PartitionLimiterState state)
    {
        return _limiters.TryGetValue(partitionKey, out state!);
    }

    private static void StoreResponseMetadata(
        HttpContext httpContext,
        OAuthClientRateLimitRequestContext requestContext,
        FixedWindowRateLimiter limiter)
    {
        var remainingPermits = (int)(limiter.GetStatistics()?.CurrentAvailablePermits ?? 0);

        httpContext.Items[ResponseMetadataKey] = new OAuthClientRateLimitResponseMetadata(
            requestContext.ClientId,
            requestContext.TenantId,
            requestContext.IpAddress,
            requestContext.Endpoint,
            requestContext.PermitLimit,
            requestContext.QueueLimit,
            requestContext.TimeWindow,
            remainingPermits);
    }

    private sealed record PartitionLimiterConfiguration(
        int PermitLimit,
        int QueueLimit,
        TimeSpan TimeWindow);

    private sealed record PartitionLimiterState(
        PartitionLimiterConfiguration Configuration,
        FixedWindowRateLimiter Limiter);

    private sealed class SuccessfulRateLimitLease : RateLimitLease
    {
        public static readonly SuccessfulRateLimitLease Instance = new();

        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => Array.Empty<string>();

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}

internal sealed record OAuthClientRateLimitResponseMetadata(
    string? ClientId,
    int? TenantId,
    string IpAddress,
    string Endpoint,
    int PermitLimit,
    int QueueLimit,
    TimeSpan TimeWindow,
    int RemainingPermits);
