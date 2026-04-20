namespace TokenIDP.Core.Abstractions.Telemetry;

public interface IRequestLatencyTelemetryStore
{
    void Record(RequestLatencyMeasurement measurement);
    RequestLatencySnapshot GetSnapshot(int? tenantId = null, string? clientId = null, TimeSpan? window = null);
}
