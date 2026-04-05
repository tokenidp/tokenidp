namespace IDP.Projection.Mappers;

public sealed record OutboxMetadata(
    int TenantId,
    string EventType,
    string AggregateType,
    string? AggregateId,
    string PartitionKey);