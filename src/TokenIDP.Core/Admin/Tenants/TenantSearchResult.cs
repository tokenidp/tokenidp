namespace TokenIDP.Core.Admin.Tenants;

public class TenantSearchResult
{
    public TenantSearchResult(int id,
        string tenantName,
        string tenantCode,
        string tenantKey,
        string? email,
        AuthenticationModes? authenticationMode,
        bool? isActive,
        bool isSystemTenant)
    {
        Id = id;
        TenantName = tenantName;
        TenantCode = tenantCode;
        TenantKey = tenantKey;
        Email = email ?? string.Empty;
        AuthenticationMode = authenticationMode;
        IsActive = isActive ?? false;
        IsSystemTenant = isSystemTenant;
    }

    public static Expression<Func<Tenant, TenantSearchResult>> Projection =>
       t => new TenantSearchResult(
           t.Id,
           t.TenantName,
           t.TenantCode,
           t.TenantKey,
           t.Email,
           t.TenantAuthSetting.AuthenticationMode,
           t.IsActive,
           t.IsSystemTenant);

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string TenantKey { get; set; }
    public string Email { get; set; }
    public AuthenticationModes? AuthenticationMode { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemTenant { get; set; }
}
