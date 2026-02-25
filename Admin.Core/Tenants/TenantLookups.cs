namespace Admin.Core.Tenants;

internal class TenantLookups
{
    public List<LookupItem> Statuses { get; init; } = new();
    public List<LookupItem> ExternalProviders { get; init; } = new();
    public List<LookupItem> Themes { get; init; } = new();
    public List<LookupItem> AuthenticationModes { get; init; } = new();
}
