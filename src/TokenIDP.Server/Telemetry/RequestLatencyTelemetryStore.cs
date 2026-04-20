using TokenIDP.Core.Abstractions.Telemetry;

namespace TokenIDP.Server.Telemetry;

internal sealed class RequestLatencyTelemetryStore : IRequestLatencyTelemetryStore
{
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(15);
    private const int MaxSamples = 50_000;

    private readonly Queue<RequestLatencyMeasurement> _samples = new();
    private readonly object _gate = new();

    public void Record(RequestLatencyMeasurement measurement)
    {
        lock (_gate)
        {
            _samples.Enqueue(measurement);
            TrimUnsafe(measurement.TimestampUtc - DefaultWindow);

            while (_samples.Count > MaxSamples)
            {
                _samples.Dequeue();
            }
        }
    }

    public RequestLatencySnapshot GetSnapshot(int? tenantId = null, string? clientId = null, TimeSpan? window = null)
    {
        var effectiveWindow = window ?? DefaultWindow;
        var cutoffUtc = DateTime.UtcNow - effectiveWindow;

        lock (_gate)
        {
            TrimUnsafe(cutoffUtc);

            var filtered = _samples
                .Where(sample =>
                    sample.TimestampUtc >= cutoffUtc &&
                    (!tenantId.HasValue || sample.TenantId == tenantId.Value) &&
                    (string.IsNullOrWhiteSpace(clientId) ||
                     string.Equals(sample.ClientId, clientId, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (filtered.Count == 0)
            {
                return new RequestLatencySnapshot();
            }

            return new RequestLatencySnapshot
            {
                P95Ms = CalculatePercentile(filtered, 95),
                P99Ms = CalculatePercentile(filtered, 99),
                SampleCount = filtered.Count,
                ClientBreakdowns = filtered
                    .Where(sample => !string.IsNullOrWhiteSpace(sample.ClientId))
                    .GroupBy(sample => sample.ClientId, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new RequestLatencyBreakdown
                    {
                        ClientId = group.Key,
                        P95Ms = CalculatePercentile(group, 95),
                        P99Ms = CalculatePercentile(group, 99),
                        SampleCount = group.Count()
                    })
                    .OrderByDescending(item => item.SampleCount)
                    .ThenByDescending(item => item.P95Ms)
                    .Take(5)
                    .ToList()
            };
        }
    }

    private void TrimUnsafe(DateTime cutoffUtc)
    {
        while (_samples.Count > 0 && _samples.Peek().TimestampUtc < cutoffUtc)
        {
            _samples.Dequeue();
        }
    }

    private static double CalculatePercentile(IEnumerable<RequestLatencyMeasurement> samples, int percentile)
    {
        var ordered = samples
            .Select(sample => sample.DurationMs)
            .OrderBy(value => value)
            .ToArray();

        if (ordered.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling((percentile / 100d) * ordered.Length) - 1;
        index = Math.Clamp(index, 0, ordered.Length - 1);

        return Math.Round(ordered[index], 2);
    }
}
