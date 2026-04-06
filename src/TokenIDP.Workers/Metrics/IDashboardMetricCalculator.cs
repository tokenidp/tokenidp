namespace TokenIDP.Workers.Metrics;

public interface IDashboardMetricCalculator
{
    Task CalculateAsync(
        ApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct);
}
