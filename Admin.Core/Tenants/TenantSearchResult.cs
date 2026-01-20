using IDP.Domain.Specifications;

namespace Admin.Core.Tenants;

internal class TenantSearchResult
{
    internal TenantSearchResult(int id,
        string tenantName,
        string tenantCode,
        string? email,
        TenantTypes? tenantType,
        SubscriptionTypes? subscriptionType,
        AuthenticationModes? authenticationMode,
        bool? isActive)
    {
        Id = id;
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email ?? string.Empty;
        TenantType = tenantType;
        SubscriptionType = subscriptionType;
        AuthenticationMode = authenticationMode;
        IsActive = isActive ?? false;
    }

    internal static Expression<Func<Tenant, TenantSearchResult>> Projection =>
       t => new TenantSearchResult(
           t.Id,
           t.TenantName,
           t.TenantCode,
           t.Email,
           t.TenantType,
           t.SubscriptionType,
           t.AuthenticationMode,
           t.IsActive);

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public TenantTypes? TenantType { get; set; }
    public SubscriptionTypes? SubscriptionType { get; set; }
    public AuthenticationModes? AuthenticationMode { get; set; }
    public bool IsActive { get; set; }
}