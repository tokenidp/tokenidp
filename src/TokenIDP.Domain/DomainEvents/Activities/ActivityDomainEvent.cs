using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Domain.DomainEvents.Activities;

public sealed record ActivityDomainEvent(
    int TenantId,
    ActivityEventType EventType,
    string AggregateType,
    string? AggregateId,
    string? ActorId,
    string? ActorDisplayName,
    string? TargetId,
    string? TargetDescription,
    string? Status,
    string? Description,
    Guid? CorrelationId = null,
    string? IpAddress = null,
    string? UserAgent = null) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
