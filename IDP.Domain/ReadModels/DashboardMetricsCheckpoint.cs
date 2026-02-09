namespace IDP.Domain.ReadModels;

public sealed class DashboardMetricsCheckpoint
{
    public string MetricKey { get; private set; } = string.Empty;

    public DateTime LastProcessedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private DashboardMetricsCheckpoint() { }

    private DashboardMetricsCheckpoint(string metricKey, DateTime lastProcessedUtc)
    {
        MetricKey = metricKey;
        LastProcessedAt = lastProcessedUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public static DashboardMetricsCheckpoint Create(
        string metricKey,
        DateTime lastProcessedUtc)
    {
        if (string.IsNullOrWhiteSpace(metricKey))
            throw new ArgumentException("MetricKey cannot be empty", nameof(metricKey));

        return new DashboardMetricsCheckpoint(metricKey, lastProcessedUtc);
    }

    public void AdvanceTo(DateTime processedUntilUtc)
    {
        if (processedUntilUtc < LastProcessedAt)
            throw new InvalidOperationException(
                "Checkpoint cannot move backwards");

        LastProcessedAt = processedUntilUtc;
        UpdatedAt = DateTime.UtcNow;
    }
}