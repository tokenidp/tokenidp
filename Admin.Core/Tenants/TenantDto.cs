namespace Admin.Core.Tenants;

internal class TenantDto
{
    internal static Expression<Func<Tenant, TenantDto>> Projection =>
      t => new TenantDto
      {
          Id = t.Id,
          Email = t.Email,
          IsActive = t.IsActive,
          HomePageUrl = t.HomePageUrl,
          TenantName = t.TenantName,
          TenantCode = t.TenantCode,
          Theme = t.Theme,
          Logo = t.LogoUrl,
      };

    public int Id { get; set; }
    public required string TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string? Email { get; set; }
    public string? Theme { get; set; }
    public string? Logo { get; set; }
    public string? HomePageUrl { get; set; }
    public bool? IsActive { get; set; }
}
