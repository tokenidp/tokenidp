using IDP.Domain.ReadModels.Enums;

namespace IDP.Projection.Metrics;

internal static class DashboardMetricRankingWriter
{
    public static async Task UpsertAsync(
        ApplicationDbContext db,
        int tenantId,
        string metricKey,
        TimeBucketType bucketType,
        DateTime bucketStartUtc,
        int rank,
        string dimension,
        int value,
        CancellationToken ct)
    {
        var existing = await db.DashboardMetricRankings.SingleOrDefaultAsync(r =>
            r.TenantId == tenantId &&
            r.MetricKey == metricKey &&
            r.BucketType == bucketType &&
            r.BucketStart == bucketStartUtc &&
            r.Rank == rank,
            ct);

        if (existing == null)
        {
            var ranking = DashboardMetricRanking.Create(
                tenantId,
                metricKey,
                bucketType,
                bucketStartUtc,
                rank,
                dimension,
                value);

            db.DashboardMetricRankings.Add(ranking);
        }
        else
        {
            existing.UpdateRank(rank, value);
        }
    }
}

