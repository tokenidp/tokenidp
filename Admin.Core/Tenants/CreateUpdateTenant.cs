using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Tenants;

public class CreateUpdateTenant
{
    public CreateUpdateTenant(string tenantName, 
        string? tenantCode, 
        string? email, 
        string? theme, 
        string? logoUrl,
        string? primaryColor, 
        string? defaultLanguage, 
        string? loginText, 
        bool? twoFactorEnabled, 
        int? twoFactorCodeExpiry, 
        string? homePageUrl, 
        bool isActive, 
        TenantTypes tenantType, 
        SubscriptionTypes subscriptionType,
        AuthenticationModes authenticationMode)
    {
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email;
        Theme = theme;
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        DefaultLanguage = defaultLanguage;
        LoginText = loginText;
        TwoFactorEnabled = twoFactorEnabled;
        TwoFactorCodeExpiry = twoFactorCodeExpiry;
        HomePageUrl = homePageUrl;
        IsActive = isActive;
        TenantType = tenantType;
        SubscriptionType = subscriptionType;
        AuthenticationMode = authenticationMode;
    }

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Theme { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? LoginText { get; set; }
    public bool? TwoFactorEnabled { get; set; }
    public int? TwoFactorCodeExpiry { get; set; }
    public string? HomePageUrl { get; set; }
    public bool IsActive { get; set; }
    public TenantTypes TenantType { get; set; }
    public SubscriptionTypes SubscriptionType { get; set; }
    public AuthenticationModes AuthenticationMode { get; set; }
}