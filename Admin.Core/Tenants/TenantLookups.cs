namespace Admin.Core.Tenants;

internal class TenantLookups
{
    public List<LookupItem> Statuses { get; init; } = new();
    public List<LookupItem> TenantTypes { get; init; } = new();
    public List<LookupItem> SubscriptionTypes { get; init; } = new();
    public List<LookupItem> AuthenticationModes { get; init; } = new();
}
