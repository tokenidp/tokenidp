namespace IDP.Domain.AggregateRoots.Tenants;

public partial class Tenant : BaseEntity, IAggregateRoot
{
    public string TenantName { get; private set; }
    public string TenantCode { get; private set; }
    public string? Email { get; private set; }
    public string? Theme { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? DefaultLanguage { get; private set; }
    public string? LoginText { get; private set; }
    public bool? TwoFactorEnabled { get; private set; }
    public int? TwoFactorCodeExpiry { get; private set; }
    public string? HomePageUrl { get; private set; }
    public bool? IsActive { get; private set; }
    public virtual ICollection<Client> Clients { get; private set; }
    public virtual ICollection<Configuration> Configurations { get; private set; }
    public virtual ICollection<User> Users { get; private set; }
    public virtual ICollection<Role> Roles { get; private set; }
    public virtual ICollection<TenantPermission> TenantPermissions { get; private set; }

    private Tenant() { }

    public Tenant(string tenantName,
        string tenantCode,
        string? email,
        string? theme,
        string? logo,
        string? landingPage,
        bool? isActive)
    {
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email;
        Theme = theme;
        LogoUrl = logo;
        HomePageUrl = landingPage;
        IsActive = isActive;

        Roles = new List<Role>();
        TenantPermissions = new List<TenantPermission>();
        Configurations = new List<Configuration>();
    }

    public void UpdateTenant(string tenantName,
        string? email,
        string? theme,
        string? logo,
        string? landingPage,
        bool? isActive)
    {
        TenantName = tenantName;
        Email = email;
        Theme = theme;
        LogoUrl = logo;
        HomePageUrl = landingPage;
        IsActive = isActive;
    }

    public void AddTenantRoles(string name, string description)
    {
        Role appRole = new
            (
                default,
                name,
                description,
                true
            );

        Roles.Add(appRole);
    }

    public void AddTenantClaims(int appClaimId, string claimType)
    {
        TenantPermission appClaimTenant = new(appClaimId, claimType);
        TenantPermissions.Add(appClaimTenant);
    }

    public void AddTenantConfigurations(string configKey,
        string configValue,
        bool? isDisplay,
        bool isDefaultforTenant)
    {
        Configuration appConfiguration = new
            (
                default,
                configKey,
                configValue,
                isDisplay,
                isDefaultforTenant
            );

        Configurations.Add(appConfiguration);
    }
}
