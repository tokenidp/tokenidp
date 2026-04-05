using IDP.Domain.ReadModels.Enums;

namespace IDP.Domain.ReadModels;

public sealed class DashboardMetric : Entity<long>, ITenant
{
    public int TenantId { get; private set; }

    public string MetricKey { get; private set; } = default!;
    public string? DimensionKey { get; private set; } = default!;
    public TimeBucketType BucketType { get; private set; }

    public DateTime BucketStart { get; private set; }
    public DateTime BucketEnd { get; private set; }

    public int MetricValue { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private DashboardMetric() { }

    private DashboardMetric(
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        DateTime bucketEndUtc,
        string? dimension,
        int value)
    {
        TenantId = tenantId;
        MetricKey = metricKey;
        BucketType = bucketType;
        BucketStart = bucketStartUtc;
        BucketEnd = bucketEndUtc;
        DimensionKey = dimension;
        MetricValue = value;
        CreatedAt = DateTime.UtcNow;
    }

    public static DashboardMetric Create(
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        DateTime bucketEndUtc,
        string? dimension,
        int value)
    {
        if (bucketEndUtc <= bucketStartUtc)
            throw new DomainException("Bucket end must be after bucket start");

        if (value < 0)
            throw new DomainException("Metric value cannot be negative");

        return new DashboardMetric(
            tenantId,
            metricKey,
            bucketType,
            bucketStartUtc,
            bucketEndUtc,
            dimension,
            value);
    }

    public void Recalculate(int newValue, DateTime newBucketEndUtc)
    {
        if (newValue < 0)
            throw new DomainException("Metric value cannot be negative");

        MetricValue = newValue;
        BucketEnd = newBucketEndUtc;
    }
}

public sealed record MetricDimension(string Value)
{
    public static string Client(long clientId)
        => new($"client:{clientId}");

    public static string User(long userId)
        => new($"user:{userId}");

    public static string Ip(string ip)
        => new($"ip:{ip}");

    public static string ClientGrant(long clientId, string grantType)
    => new($"client:{clientId}|grant:{grantType}");

    public static string GrantType(string grantType)
   => new($"grant:{grantType}");

    public static string None()
        => new(string.Empty);

    public bool IsNone => string.IsNullOrEmpty(Value);

    public override string ToString() => Value;
}