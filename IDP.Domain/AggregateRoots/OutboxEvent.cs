using IDP.Domain.Specifications;
using System.Text.Json;

namespace IDP.Domain.AggregateRoots;

public sealed class OutboxEvent : AggregateRoot<long>
{
    public int TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }
    public OutboxStatus Status { get; private set; }
    public string AggregateId { get; private set; }
    public string? PartitionKey { get; private set; }

    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    private OutboxEvent() { }

    private OutboxEvent(
        int tenantId,
        string eventType,
        string aggregateId,
        string payloadJson,
        string? partitionKey)
    {
        TenantId = tenantId;
        EventType = eventType;
        AggregateId = aggregateId;
        PayloadJson = payloadJson;
        PartitionKey = partitionKey;

        CreatedAt = DateTime.UtcNow;
        Status = OutboxStatus.Pending;
        RetryCount = 0;
    }

    public static OutboxEvent Create(
        int tenantId,
        string eventType,
        string aggregateId,
        object payload,
        string? partitionKey = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new DllNotFoundException("EventType is required");

        var json = JsonSerializer.Serialize(payload);

        return new OutboxEvent(
            tenantId,
            eventType,
            aggregateId,
            json,
            partitionKey);
    }

    public void MarkProcessed()
    {
        if (Status == OutboxStatus.Processed)
            return;

        Status = OutboxStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        Error = null;
        NextAttemptAt = null;
        LockedUntil = null;
        LockedBy = null;
        FailedAt = null;
    }

    public void MarkFailed(DateTime utcNow, string error, TimeSpan delay, int maxRetries)
    {
        RetryCount++;
        Error = error.Length > 1024 ? error[..1024] : error;

        if (RetryCount >= maxRetries)
        {
            FailedAt = utcNow;
            Status = OutboxStatus.Failed;
            // leave ProcessedAt null to indicate “not processed”; FailedAt indicates DLQ state
            NextAttemptAt = null;
        }
        else
        {
            NextAttemptAt = utcNow.Add(delay);
        }

        LockedUntil = null;
        LockedBy = null;
    }

    public void Lock(string workerId, DateTime utcNow, TimeSpan lockDuration)
    {
        LockedBy = workerId;
        LockedUntil = utcNow.Add(lockDuration);
    }

    public bool CanProcess(DateTime nowUtc)
    {
        if (Status == OutboxStatus.Processed || Status == OutboxStatus.Failed)
            return false;

        if (NextAttemptAt.HasValue && NextAttemptAt > nowUtc)
            return false;

        return true;
    }
}

public static class OutboxEventFactory
{
    public static OutboxEvent CreateFromDomainEvent(IDomainEvent domainEvent,
        string? partitionKey = null)
    {
        return OutboxEvent.Create(
            domainEvent.TenantId,
            domainEvent.EventType,
            domainEvent.AggregateId,
            domainEvent,
            partitionKey
        );
    }
}
