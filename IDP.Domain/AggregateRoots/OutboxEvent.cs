using IDP.Domain.DomainEvents;
using IDP.Domain.Specifications;
using System.Text.Json;

namespace IDP.Domain.AggregateRoots;

public sealed class OutboxEvent : AggregateRoot<long>
{
    public int TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;

    public long AggregateId { get; private set; }
    public string? PartitionKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }

    public OutboxStatus Status { get; private set; }

    private OutboxEvent() { }

    private OutboxEvent(
        int tenantId,
        string eventType,
        long aggregateId,
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
        long aggregateId,
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
    }

    public void MarkFailed(Exception ex, int maxRetries)
    {
        RetryCount++;
        Error = ex.Message;

        if (RetryCount >= maxRetries)
        {
            Status = OutboxStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            Status = OutboxStatus.Pending;
            NextAttemptAt = DateTime.UtcNow.AddSeconds(
                Math.Pow(2, RetryCount));
        }
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
    public static OutboxEvent CreateFromDomainEvent(
        IDomainEvent domainEvent,
        int tenantId,
        long? aggregateId,
        string? partitionKey = null)
    {
        return OutboxEvent.Create(
            tenantId,
            MapEventType(domainEvent),
            aggregateId ?? 0,
            domainEvent,
            partitionKey
        );
    }

    private static string MapEventType(IDomainEvent evt) =>
        evt switch
        {
            TokenIssuedDomainEvent => OutboxEventTypes.TokenIssued,
            TokenRevokedDomainEvent => OutboxEventTypes.TokenRevoked,
            TokenRefreshRotatedDomainEvent => OutboxEventTypes.TokenRefreshRotated,
            TokenRefreshReuseDetectedDomainEvent => OutboxEventTypes.TokenRefreshReuseDetected,
            ReferenceTokenIssuedDomainEvent  => OutboxEventTypes.ReferenceTokenIssued,
            RefreshTokenIssuedDomainEvent => OutboxEventTypes.RefreshTokenIssued,
            _ => throw new InvalidOperationException(
                $"No outbox mapping defined for {evt.GetType().Name}")
        };
}
