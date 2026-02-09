namespace Notification.Core.Abstractions;

public interface IRetrySchedule
{
    DateTime ComputeNextAttemptUtc(int attemptCount, DateTime nowUtc);
}
