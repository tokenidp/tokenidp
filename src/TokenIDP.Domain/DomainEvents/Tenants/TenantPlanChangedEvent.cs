namespace TokenIDP.Domain.DomainEvents.Tenants;

public sealed record TenantPlanChangedEvent(
    int TenantId,
    string TenantKey,
    string PlanCode) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
