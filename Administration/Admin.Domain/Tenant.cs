using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.Entities;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public partial class Tenant : BaseEntity, IAggregateRoot
{
    public string TenantName { get; private set; }
    public string TenantCode { get; private set; }
    public string Email { get; private set; }
    public string Theme { get; private set; }
    public string Logo { get; private set; }
    public string TenantAppId { get; private set; }
    public string LandingPage { get; private set; }
    public bool? IsActive { get; private set; }
    public bool IsParentTenant { get; private set; } = false;
    public virtual ICollection<AppConfiguration> AppConfigurations { get; private set; }
    public virtual ICollection<AppUser> AppUsers { get; private set; }
    public virtual ICollection<AppRole> AppRoles { get; private set; }
    public virtual ICollection<AppClaimTenant> AppClaimTenants { get; private set; }

    private Tenant() { }

    public Tenant(string tenantName,
        string tenantCode,
        string email,
        string theme,
        string logo,
        string tenantAppId,
        string landingPage,
        bool? isActive)
    {
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email;
        Theme = theme;
        Logo = logo;
        TenantAppId = tenantAppId;
        LandingPage = landingPage;
        IsActive = isActive;

        AppRoles = new List<AppRole>();
        AppClaimTenants = new List<AppClaimTenant>();
        AppConfigurations = new List<AppConfiguration>();
    }

    public void UpdateTenant(string tenantName,
        string email,
        string theme,
        string logo,
        string tenantAppId,
        string landingPage,
        bool? isActive)
    {
        TenantName = tenantName;
        Email = email;
        Theme = theme;
        Logo = logo;
        TenantAppId = tenantAppId;
        LandingPage = landingPage;
        IsActive = isActive;
    }

    public void AddTenantRoles(string name, string description)
    {
        AppRole appRole = new
            (
                default,
                name,
                description,
                true
            );

        AppRoles.Add(appRole);
    }

    public void AddTenantClaims(int appClaimId, string claimType)
    {
        AppClaimTenant appClaimTenant = new(appClaimId, claimType);
        AppClaimTenants.Add(appClaimTenant);
    }

    public void AddTenantConfigurations(string configKey,
        string configValue,
        bool? isDisplay,
        bool isDefaultforTenant)
    {
        AppConfiguration appConfiguration = new
            (
                default,
                configKey,
                configValue,
                isDisplay,
                isDefaultforTenant
            );

        AppConfigurations.Add(appConfiguration);
    }
}
