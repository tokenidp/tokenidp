namespace TokenIDP.Infrastructure.Emails.Abstractions;

public interface IRetrySchedule
{
    DateTime ComputeNextAttemptUtc(int attemptCount, DateTime nowUtc);
}

