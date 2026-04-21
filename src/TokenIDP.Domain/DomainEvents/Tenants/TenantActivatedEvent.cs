namespace TokenIDP.Domain.DomainEvents.Tenants;

public sealed record TenantActivatedEvent(
    int TenantId,
    string TenantKey,
    bool IsSystemTenant) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
