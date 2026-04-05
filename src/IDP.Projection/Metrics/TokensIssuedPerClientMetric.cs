using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

public sealed class TokensIssuedPerClientMetric : IDashboardMetricCalculator
{
    public async Task CalculateAsync(
        ApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct)
    {
        var grouped = await db.TokenReadModel
            .Where(t =>
                t.TenantId == tenantId &&
                t.CreatedOn >= bucketStart &&
                t.CreatedOn < bucketEnd)
            .GroupBy(t => t.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        foreach (var row in grouped)
        {
            await DashboardMetricWriter.UpsertAsync(
                db,
                tenantId,
                MetricType.TokensIssuedPerClient,
                TimeBucketType.Hour,
                bucketStart,
                bucketEnd,
                row.Count,
                MetricDimension.Client(row.ClientId),
                ct);
        }
    }
}