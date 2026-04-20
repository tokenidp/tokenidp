namespace TokenIDP.Core.Abstractions.Telemetry;

public sealed class RequestLatencySnapshot
{
    public double P95Ms { get; init; }
    public double P99Ms { get; init; }
    public int SampleCount { get; init; }
    public IReadOnlyList<RequestLatencyBreakdown> ClientBreakdowns { get; init; } = [];
}

public sealed class RequestLatencyBreakdown
{
    public string ClientId { get; init; } = string.Empty;
    public double P95Ms { get; init; }
    public double P99Ms { get; init; }
    public int SampleCount { get; init; }
}
