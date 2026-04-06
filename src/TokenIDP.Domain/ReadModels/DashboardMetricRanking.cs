using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Domain.ReadModels;

public sealed class DashboardMetricRanking : Entity<long>, ITenant
{
    public int TenantId { get; private set; }

    public string MetricKey { get; private set; } = default!;
    public string DimensionKey { get; private set; } = default!;
    public TimeBucketType BucketType { get; private set; }

    public DateTime BucketStart { get; private set; }

    public int Rank { get; private set; }

    public int MetricValue { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private DashboardMetricRanking() { }

    private DashboardMetricRanking(
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        int rank,
        string dimension,
        int value)
    {
        TenantId = tenantId;
        MetricKey = metricKey;
        BucketType = bucketType;
        BucketStart = bucketStartUtc;
        Rank = rank;
        DimensionKey = dimension;
        MetricValue = value;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static DashboardMetricRanking Create(
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        int rank,
        string dimension,
        int value)
    {
        if (rank <= 0) throw new InvalidOperationException("Rank must be >= 1");
        if (value < 0) throw new InvalidOperationException("Value cannot be negative");

        return new DashboardMetricRanking(
            tenantId,
            metricKey,
            bucketType,
            bucketStartUtc,
            rank,
            dimension,
            value);
    }

    public void UpdateRank(int newRank, int newValue)
    {
        if (newRank <= 0) throw new InvalidOperationException("Rank must be >= 1");
        if (newValue < 0) throw new InvalidOperationException("Value cannot be negative");

        Rank = newRank;
        MetricValue = newValue;
    }
}


