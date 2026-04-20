using System.Diagnostics;
using System.Diagnostics.Metrics;
using TokenIDP.Core.Abstractions.Telemetry;

namespace TokenIDP.Server.Telemetry;

internal static class RequestLatencyMetrics
{
    public const string MeterName = "TokenIDP.Server.RequestLatency";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
        "tokenidp.http.server.duration",
        unit: "ms",
        description: "HTTP request duration with tenant and client dimensions.");

    public static void Record(RequestLatencyMeasurement measurement)
    {
        var tags = new TagList
        {
            { "http.method", measurement.Method },
            { "http.route", measurement.Route },
            { "http.status_code", measurement.StatusCode },
            { "tenant.id", measurement.TenantId?.ToString() ?? "unknown" },
            { "client.id", string.IsNullOrWhiteSpace(measurement.ClientId) ? "unknown" : measurement.ClientId }
        };

        DurationHistogram.Record(measurement.DurationMs, tags);
    }
}
