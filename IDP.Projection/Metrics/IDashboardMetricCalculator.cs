namespace IDP.Projection.Metrics;

public interface IDashboardMetricCalculator
{
    Task CalculateAsync(
        IApplicationDbContext db,
        int tenantId,
        DateTime bucketStart,
        DateTime bucketEnd,
        CancellationToken ct);
}