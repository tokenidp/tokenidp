namespace TokenIDP.Core.Admin.Tenants;

public class TenantSearchResult
{
    public TenantSearchResult(int id,
        string tenantName,
        string tenantCode,
        string? email,
        AuthenticationModes? authenticationMode,
        bool? isActive)
    {
        Id = id;
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email ?? string.Empty;
        AuthenticationMode = authenticationMode;
        IsActive = isActive ?? false;
    }

    public static Expression<Func<Tenant, TenantSearchResult>> Projection =>
       t => new TenantSearchResult(
           t.Id,
           t.TenantName,
           t.TenantCode,
           t.Email,
           t.TenantAuthSetting.AuthenticationMode,
           t.IsActive);

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public AuthenticationModes? AuthenticationMode { get; set; }
    public bool IsActive { get; set; }
}
