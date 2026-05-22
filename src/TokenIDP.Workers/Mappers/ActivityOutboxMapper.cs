using TokenIDP.Domain.DomainEvents.Activities;

namespace TokenIDP.Workers.Mappers;

internal sealed class ActivityOutboxMapper : IOutboxMapper
{
    public bool CanHandle(IDomainEvent evt)
        => evt is ActivityDomainEvent;

    public OutboxEvent Map(IDomainEvent evt)
        => evt switch
        {
            ActivityDomainEvent e => OutboxEvent.Create(
                tenantId: e.TenantId,
                eventType: e.EventType.ToString(),
                aggregateId: e.AggregateId ?? e.TargetId ?? string.Empty,
                aggregateType: e.AggregateType,
                payload: e,
                partitionKey: $"tenant:{e.TenantId}:activity"),
            _ => throw new InvalidOperationException()
        };
}
