namespace IDP.Domain.Base;

public interface IDomainEvent
{
    int TenantId { get; }
    string AggregateId { get; }
    string AggregateType { get; }
    string EventType { get; }
    DateTime OccurredOn { get; }
}