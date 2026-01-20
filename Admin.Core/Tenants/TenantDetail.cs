using IDP.Domain.Specifications;

namespace Admin.Core.Tenants;

internal class TenantDetail
{
    internal static Expression<Func<Tenant, TenantDetail>> Projection =>
      t => new TenantDetail
      {
          Id = t.Id,
          Email = t.Email,
          IsActive = t.IsActive,
          HomePageUrl = t.HomePageUrl,
          TenantName = t.TenantName,
          TenantCode = t.TenantCode,
          Theme = t.Theme,
          LogoUrl = t.LogoUrl,
          PrimaryColor = t.PrimaryColor,
          DefaultLanguage = t.DefaultLanguage,
          LoginText = t.LoginText,
          TwoFactorEnabled = t.TwoFactorEnabled,
          TwoFactorCodeExpiry = t.TwoFactorCodeExpiry,
          TenantType = t.TenantType,
          SubscriptionType = t.SubscriptionType,
          AuthenticationMode = t.AuthenticationMode,
      };

    public int Id { get; set; }
    public required string TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string? Email { get; set; }
    public string? Theme { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? LoginText { get; set; }
    public bool? TwoFactorEnabled { get; set; }
    public int? TwoFactorCodeExpiry { get; set; }
    public string? HomePageUrl { get; set; }
    public bool? IsActive { get; set; }
    public TenantTypes TenantType { get; set; }
    public SubscriptionTypes SubscriptionType { get; set; }
    public AuthenticationModes AuthenticationMode { get; set; }
}