namespace TokenIDP.Domain.DomainEvents.Tenants;

public sealed record TenantBrandingChangedEvent(
    int TenantId,
    string TenantKey) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
