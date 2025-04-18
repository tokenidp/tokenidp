namespace Identity.Application.Tenants.Queries;

public class TenantDto
{
    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public string Theme { get; set; }
    public string Logo { get; set; }
    public string TenantAppId { get; set; }
    public string LandingPage { get; set; }
    public bool? IsActive { get; set; }
    public bool IsParentTenant { get; set; }
}
