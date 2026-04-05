using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

internal static class DashboardMetricWriter
{
    public static async Task UpsertAsync(
        ApplicationDbContext db,
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        DateTime bucketEndUtc,
        int value,
        string? dimension,
        CancellationToken ct)
    {
        if (dimension == string.Empty)
        {
            dimension = null;
        }
        var existing = await db.DashboardMetrics
            .SingleOrDefaultAsync(m =>
                m.TenantId == tenantId &&
                m.MetricKey == metricKey &&
                m.BucketType == bucketType &&
                m.BucketStart == bucketStartUtc &&
                m.DimensionKey == dimension,
                ct);

        if (existing == null)
        {
            var metric = DashboardMetric.Create(
                tenantId,
                metricKey,
                bucketType,
                bucketStartUtc,
                bucketEndUtc,
                dimension,
                value);

            db.DashboardMetrics.Add(metric);
        }
        else
        {
            existing.Recalculate(value, bucketEndUtc);
        }
    }
}
