namespace TokenIDP.Infrastructure.Outbox;

public sealed record OutboxMetadata(
    int TenantId,
    string EventType,
    string AggregateType,
    string? AggregateId,
    string PartitionKey);
