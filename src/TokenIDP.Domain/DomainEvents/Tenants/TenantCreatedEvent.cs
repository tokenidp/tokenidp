namespace TokenIDP.Domain.DomainEvents.Tenants;

public sealed record TenantCreatedEvent(
    int TenantId,
    string TenantKey,
    bool IsSystemTenant) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
