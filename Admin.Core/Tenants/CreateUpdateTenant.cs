namespace Admin.Core.Tenants;

internal class CreateUpdateTenant
{
    public int Id { get; set; }
    public required string TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string? Email { get; set; }
    public string? Theme { get; set; }
    public string? Logo { get; set; }
    public string? LandingPage { get; set; }
    public bool? IsActive { get; set; }
}
