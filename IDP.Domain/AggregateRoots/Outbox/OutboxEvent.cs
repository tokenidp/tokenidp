using System.Text.Json;

namespace IDP.Domain.AggregateRoots.Outbox;

public sealed class OutboxEvent : AggregateRoot<long>
{
    private readonly List<OutboxEventConsumer> _consumers = new();

    public int TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string AggregateId { get; private set; } = default!;
    public string AggregateType { get; private set; } = default!;
    public string? PartitionKey { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<OutboxEventConsumer> OutboxEventConsumers => _consumers.AsReadOnly();

    private OutboxEvent() { }

    public static OutboxEvent Create(
        int tenantId,
        string eventType,
        string aggregateId,
        string aggregateType,
        object payload,
        string? partitionKey = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new DllNotFoundException("EventType is required");

        var json = JsonSerializer.Serialize(payload);

        return new OutboxEvent()
        {
            TenantId = tenantId,
            EventType = eventType,
            AggregateId = aggregateId,
            PayloadJson = json,
            PartitionKey = partitionKey,
            CreatedAt = DateTime.UtcNow,
            AggregateType = aggregateType
        };
    }

    public void AddConsumer(string consumerName)
    {
        _consumers.Add(new OutboxEventConsumer(consumerName));
    }
}
