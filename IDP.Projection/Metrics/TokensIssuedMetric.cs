using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

public sealed class TokensIssuedMetric : IDashboardMetricCalculator
{
    public async Task CalculateAsync(
        IApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct)
    {
        var counts = await db.TokenReadModel
            .Where(t =>
                t.TenantId == tenantId &&
                t.CreatedOn >= bucketStart &&
                t.CreatedOn < bucketEnd)
            .GroupBy(a => a.SourceType)
            .Select(g => new
            {
                SourceType = g.Key,
                Count = g.Count()
            }).ToListAsync(ct);

        var accessTokenCount = counts.FirstOrDefault(x => x.SourceType == "JWT" || x.SourceType == "Reference")?.Count ?? 0;
        var refreshTokenCount = counts.FirstOrDefault(x => x.SourceType == "Refresh")?.Count ?? 0;

        await DashboardMetricWriter.UpsertAsync(
            db,
            tenantId,
            MetricType.TokensIssued,
            TimeBucketType.Hour,
            bucketStart,
            bucketEnd,
            accessTokenCount,
            MetricDimension.None(),
            ct);

        await DashboardMetricWriter.UpsertAsync(
            db,
            tenantId,
            MetricType.RefreshTokensIssued,
            TimeBucketType.Hour,
            bucketStart,
            bucketEnd,
            refreshTokenCount,
            MetricDimension.None(),
            ct);

        var stats = await db.TokenReadModel
            .Where(t =>
                t.TenantId == tenantId &&
                t.CreatedOn >= bucketStart &&
                t.CreatedOn < bucketEnd)
            .GroupBy(t => t.GrantType)
            .Select(g => new
            {
                GrantType = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        foreach (var row in stats)
        {
            await DashboardMetricWriter.UpsertAsync(
                 db,
                 tenantId,
                 MetricType.TokensIssuedPerGrant,
                 TimeBucketType.Hour,
                 bucketStart,
                 bucketEnd,
                 row.Count,
                 MetricDimension.GrantType(row.GrantType),
                 ct);
        }
    }
}
