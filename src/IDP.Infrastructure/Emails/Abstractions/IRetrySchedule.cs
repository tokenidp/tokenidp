namespace IDP.Infrastructure.Emails.Abstractions;

public interface IRetrySchedule
{
    DateTime ComputeNextAttemptUtc(int attemptCount, DateTime nowUtc);
}
