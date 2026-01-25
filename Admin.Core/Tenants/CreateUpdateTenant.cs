using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Tenants;

internal class CreateUpdateTenant
{
    public int Id { get; set; }
    [Required]
    public required string TenantName { get; set; }
    public string? TenantCode { get; set; }
    [EmailAddress]
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