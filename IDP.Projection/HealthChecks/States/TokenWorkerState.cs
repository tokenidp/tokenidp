namespace IDP.Projection.HealthChecks.States;

public sealed class TokenWorkerState
{
    private long _lastHeartbeatTicks;

    public DateTime LastHeartbeatUtc
        => new DateTime(Interlocked.Read(ref _lastHeartbeatTicks), DateTimeKind.Utc);

    public void Heartbeat()
        => Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
}
