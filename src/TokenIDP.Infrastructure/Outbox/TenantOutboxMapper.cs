using TokenIDP.Domain.AggregateRoots.Outbox;
using TokenIDP.Domain.Base;
using TokenIDP.Domain.DomainEvents.Tenants;

namespace TokenIDP.Infrastructure.Outbox;

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

    private static OutboxEvent Create(IDomainEvent evt)
    {
        var metadata = ResolveMetadata(evt);

        return OutboxEvent.Create(
            tenantId: metadata.TenantId,
            eventType: metadata.EventType,
            aggregateId: metadata.AggregateId ?? string.Empty,
            aggregateType: metadata.AggregateType,
            payload: evt,
            partitionKey: metadata.PartitionKey);
    }

    private static OutboxMetadata ResolveMetadata(IDomainEvent evt)
    {
        return evt switch
        {
            TenantCreatedEvent e => new OutboxMetadata(
                e.TenantId,
                nameof(TenantCreatedEvent),
                "Tenant",
                e.TenantId.ToString(),
                $"tenant:{e.TenantId}:tenant"),
            TenantActivatedEvent e => new OutboxMetadata(
                e.TenantId,
                nameof(TenantActivatedEvent),
                "Tenant",
                e.TenantId.ToString(),
                $"tenant:{e.TenantId}:tenant"),
            TenantInactivatedEvent e => new OutboxMetadata(
                e.TenantId,
                nameof(TenantInactivatedEvent),
                "Tenant",
                e.TenantId.ToString(),
                $"tenant:{e.TenantId}:tenant"),
            TenantBrandingChangedEvent e => new OutboxMetadata(
                e.TenantId,
                nameof(TenantBrandingChangedEvent),
                "Tenant",
                e.TenantId.ToString(),
                $"tenant:{e.TenantId}:tenant"),
            TenantPlanChangedEvent e => new OutboxMetadata(
                e.TenantId,
                nameof(TenantPlanChangedEvent),
                "Tenant",
                e.TenantId.ToString(),
                $"tenant:{e.TenantId}:tenant"),
            _ => throw new InvalidOperationException(
                $"No outbox mapping defined for {evt.GetType().Name}")
        };
    }
}
