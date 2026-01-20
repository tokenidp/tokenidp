using IDP.Domain.AggregateRoots.Permissions;

using IDP.Domain.Base;
using IDP.Domain.Specifications;

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
    public TenantTypes TenantType { get; private set; }
    public SubscriptionTypes SubscriptionType { get; private set; }
    public AuthenticationModes AuthenticationMode { get; private set; }
    public bool IsActive { get; private set; }
    public virtual ICollection<Client> Clients { get; private set; }
    public virtual ICollection<Configuration> Configurations { get; private set; }
    public virtual ICollection<User> Users { get; private set; }
    public virtual ICollection<Role> Roles { get; private set; }
    public virtual ICollection<Permission> Permissions { get; private set; }

    private Tenant() { }

    private Tenant(string tenantName,
        string tenantCode,
        string? email,
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText,
        bool? twoFactorEnabled,
        int? twoFactorCodeExpiry,
        string? landingPage,
        bool isActive,
        TenantTypes tenantType,
        SubscriptionTypes subscriptionType,
        AuthenticationModes authenticationMode)
    {
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email;
        Theme = theme;
        LogoUrl = logo;
        PrimaryColor = primaryColor;
        DefaultLanguage = defaultLanguage;
        LoginText = loginText;
        TwoFactorEnabled = twoFactorEnabled;
        TwoFactorCodeExpiry = twoFactorCodeExpiry;
        HomePageUrl = landingPage;
        TenantType = tenantType;
        SubscriptionType = subscriptionType;
        AuthenticationMode = authenticationMode;
        IsActive = isActive;

        Roles = new List<Role>();
        Permissions = new List<Permission>();
        Configurations = new List<Configuration>();
    }

    public Result UpdateTenant(string tenantName,
        string? email,
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText,
        bool? twoFactorEnabled,
        int? twoFactorCodeExpiry,
        string? landingPage,
        bool isActive,
        TenantTypes tenantType,
        SubscriptionTypes subscriptionType,
        AuthenticationModes authenticationMode)
    {
        var validation = ValidateInput(tenantName, TenantCode);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        TenantName = tenantName;
        Email = email;
        Theme = theme;
        LogoUrl = logo;
        PrimaryColor = primaryColor;
        DefaultLanguage = defaultLanguage;
        LoginText = loginText;
        TwoFactorEnabled = twoFactorEnabled;
        TwoFactorCodeExpiry = twoFactorCodeExpiry;
        HomePageUrl = landingPage;
        TenantType = tenantType;
        SubscriptionType = subscriptionType;
        AuthenticationMode = authenticationMode;
        IsActive = isActive;

        return Result.Success(Id);
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

    public void AddTenantConfigurations(string configKey,
        string configValue,
        bool isDefaultforTenant)
    {
        Configuration appConfiguration = new
            (
                default,
                configKey,
                configValue,
                isDefaultforTenant
            );

        Configurations.Add(appConfiguration);
    }

    public static Result Create(string tenantName,
        string tenantCode,
        string? email,
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText,
        bool? twoFactorEnabled,
        int? twoFactorCodeExpiry,
        string? landingPage,
        bool isActive,
        TenantTypes tenantType,
        SubscriptionTypes subscriptionType,
        AuthenticationModes authenticationMode,
        out Tenant? tenant)
    {
        tenant = null;

        var validation = ValidateInput(tenantName, tenantCode);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        tenant = new Tenant(
            tenantName,
            tenantCode,
            email,
            theme,
            logo,
            primaryColor,
            defaultLanguage,
            loginText,
            twoFactorEnabled,
            twoFactorCodeExpiry,
            landingPage,
            isActive,
            tenantType,
            subscriptionType,
            authenticationMode);

        return Result.Success(0);
    }

    public Result UpdateIsActive(bool isActive)
    {
        IsActive = isActive;
        return Result.Success(Id);
    }

    public Result Disable()
    {
        return UpdateIsActive(false);
    }

    private static Result ValidateInput(string tenantName, string tenantCode)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Result.Failure("tenant.name.invalid", "Tenant name is required.");
        }

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return Result.Failure("tenant.code.invalid", "Tenant code is required.");
        }

        return Result.Success(0);
    }
}