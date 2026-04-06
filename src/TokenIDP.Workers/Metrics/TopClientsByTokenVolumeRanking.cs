using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Workers.Metrics;

public sealed class TopClientsByTokenVolumeRanking
{
    private const int TopN = 10;

    public async Task CalculateAsync(
        ApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct)
    {
        var topClients = await db.TokenReadModel
            .Where(t =>
                t.TenantId == tenantId &&
                (t.SourceType == "JWT" ||
                t.SourceType == "Reference") &&
                t.CreatedOn >= bucketStart &&
                t.CreatedOn < bucketEnd)
            .GroupBy(a => new { a.ClientId, a.GrantType })
            .Select(g => new
            {
                g.Key.ClientId,
                g.Key.GrantType,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(TopN)
            .ToListAsync(ct);

        int rank = 1;

        foreach (var row in topClients)
        {
            await DashboardMetricRankingWriter.UpsertAsync(
                db,
                tenantId,
                MetricType.TokensIssued,
                TimeBucketType.Hour,
                bucketStart,
                rank,
                MetricDimension.ClientGrant(row.ClientId, row.GrantType),
                row.Count,
                ct);

            rank++;
        }
    }
}

