namespace TokenIDP.Core.Abstractions.Telemetry;

public sealed class RequestLatencyMeasurement
{
    public DateTime TimestampUtc { get; init; }
    public double DurationMs { get; init; }
    public int? TenantId { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public int StatusCode { get; init; }
}
