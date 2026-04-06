using TokenIDP.Infrastructure.Emails.Abstractions;

namespace TokenIDP.Infrastructure.Emails.Concrete;

public sealed class ExponentialRetrySchedule : IRetrySchedule
{
    private readonly TimeSpan _min = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _max = TimeSpan.FromHours(1);

    public DateTime ComputeNextAttemptUtc(int attemptCount, DateTime nowUtc)
    {
        // attemptCount here is AFTER increment or BEFORE? choose consistently.
        // We'll assume BEFORE increment in worker; worker passes (currentAttempt + 1).
        var pow = Math.Min(10, attemptCount); // cap exponent
        var seconds = _min.TotalSeconds * Math.Pow(2, pow - 1);
        var delay = TimeSpan.FromSeconds(Math.Min(_max.TotalSeconds, seconds));

        // small jitter
        var jitterMs = Random.Shared.Next(0, 5000);
        return nowUtc.Add(delay).AddMilliseconds(jitterMs);
    }
}
