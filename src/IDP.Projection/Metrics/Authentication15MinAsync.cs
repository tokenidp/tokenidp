using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

internal sealed class Authentication15MinAsync
{
    public async Task CalculateAsync(
        ApplicationDbContext db,
        int tenantId,
        DateTime bucketStart15,
        DateTime bucketEnd15,
        CancellationToken ct)
    {
        var failedCount = await db.Activities
            .Where(a =>
                a.TenantId == tenantId &&
                a.EventType == ActivityEventType.LoginFailed &&
                a.CreatedAtUtc >= bucketStart15 &&
                a.CreatedAtUtc < bucketEnd15)
            .CountAsync(ct);

        await DashboardMetricWriter.UpsertAsync(
            db,
            tenantId,
            MetricType.MultipleFailedAttempts,
            TimeBucketType.Window15Min,
            bucketStart15,
            bucketEnd15,
            failedCount,
            MetricDimension.None(),
            ct);
    }
}
