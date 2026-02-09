namespace IDP.Domain.AggregateRoots.Outbox;

public sealed class OutboxEventConsumer
{
    public long Id { get; private set; }
    public long OutboxEventId { get; private set; }
    public string ConsumerName { get; private set; } = default!;
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }
    public string? LastError { get; private set; }

    public OutboxEvent OutboxEvent { get; private set; } = default!;

    private OutboxEventConsumer() { }

    public OutboxEventConsumer(string consumerName)
    {
        ConsumerName = consumerName;
        Status = OutboxStatus.Pending;
        RetryCount = 0;
    }

    public void MarkProcessed()
    {
        if (Status == OutboxStatus.Processed)
            return;

        Status = OutboxStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
        NextAttemptAt = null;
        LockedUntil = null;
        LockedBy = null;
        FailedAt = null;

        ClearLock();
    }

    public void MarkFailed(DateTime utcNow, string error, TimeSpan delay, int maxRetries)
    {
        RetryCount++;
        LastError = error.Length > 1024 ? error[..1024] : error;

        if (RetryCount >= maxRetries)
        {
            FailedAt = utcNow;
            Status = OutboxStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            NextAttemptAt = utcNow.Add(delay);
        }

        LockedUntil = null;
        LockedBy = null;

        ClearLock();
    }

    private void ClearLock()
    {
        LockedBy = null;
        LockedUntil = null;
    }
}
