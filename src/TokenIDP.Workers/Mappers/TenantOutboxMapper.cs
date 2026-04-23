using TokenIDP.Domain.ReadModels.Enums;

namespace TokenIDP.Workers.Mappers;

internal sealed class TenantOutboxMapper : IOutboxMapper
{
    public bool CanHandle(IDomainEvent evt)
        => evt is TenantCreatedEvent
            or TenantActivatedEvent
            or TenantInactivatedEvent
            or TenantBrandingChangedEvent
            or TenantPlanChangedEvent;

    public OutboxEvent Map(IDomainEvent evt)
        => evt switch
        {
            TenantCreatedEvent e => Create(e),
            TenantActivatedEvent e => Create(e),
            TenantInactivatedEvent e => Create(e),
            TenantBrandingChangedEvent e => Create(e),
            TenantPlanChangedEvent e => Create(e),
            _ => throw new InvalidOperationException()
        };

    private OutboxEvent Create(IDomainEvent evt)
    {
        var meta = ResolveMetaData(evt);

        return OutboxEvent.Create(
            tenantId: meta.TenantId,
            eventType: meta.EventType,
            aggregateId: meta.AggregateId!,
            aggregateType: meta.AggregateType,
            payload: evt,
            partitionKey: meta.PartitionKey);
    }

    private static OutboxMetadata ResolveMetaData(IDomainEvent evt)
    {
        return evt switch
        {
            TenantCreatedEvent e => CreateMetadata(e.TenantId, e.TenantId, ActivityEventType.TenantCreated),
            TenantActivatedEvent e => CreateMetadata(e.TenantId, e.TenantId, ActivityEventType.TenantUpdated),
            TenantInactivatedEvent e => CreateMetadata(e.TenantId, e.TenantId, ActivityEventType.TenantDisabled),
            TenantBrandingChangedEvent e => CreateMetadata(e.TenantId, e.TenantId, ActivityEventType.TenantUpdated),
            TenantPlanChangedEvent e => CreateMetadata(e.TenantId, e.TenantId, ActivityEventType.TenantUpdated),
            _ => throw new InvalidOperationException(
                $"No outbox mapping defined for {evt.GetType().Name}")
        };
    }

    private static OutboxMetadata CreateMetadata(int tenantId, int aggregateId, ActivityEventType eventType)
        => new(
            TenantId: tenantId,
            EventType: eventType.ToString(),
            AggregateType: "Tenant",
            AggregateId: aggregateId.ToString(),
            PartitionKey: $"tenant:{tenantId}:tenant");
}
