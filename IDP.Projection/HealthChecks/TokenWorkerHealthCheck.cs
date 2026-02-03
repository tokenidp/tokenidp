using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IDP.Projection.HealthChecks;

public sealed class TokenWorkerHealthCheck : IHealthCheck
{
    private static readonly TimeSpan MaxSilence = TimeSpan.FromSeconds(30);

    private readonly TokenWorkerState _state;

    public TokenWorkerHealthCheck(TokenWorkerState state)
    {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var last = _state.LastHeartbeatUtc;

        if (last == DateTime.MinValue)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Outbox worker never started"));
        }

        var silence = DateTime.UtcNow - last;

        if (silence > MaxSilence)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Outbox worker stalled. Last heartbeat {silence.TotalSeconds:N0}s ago"));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy(
                $"Outbox worker alive. Last heartbeat {silence.TotalSeconds:N0}s ago"));
    }
}
