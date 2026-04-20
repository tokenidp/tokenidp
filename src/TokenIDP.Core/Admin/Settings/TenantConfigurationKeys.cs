using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

internal static class TenantConfigurationKeys
{
    internal sealed record Definition(
        string Key,
        ValueTypes ValueType,
        ConfigurationScopes Scope,
        string Description);

    internal static readonly IReadOnlyList<Definition> Catalog = new List<Definition>
    {
        new("dashboard.region", ValueTypes.String, ConfigurationScopes.System,
            "Deployment region label shown on the dashboard."),
        new("dashboard.version", ValueTypes.String, ConfigurationScopes.System,
            "Application version label shown on the dashboard."),
        new("security.mfa.enabled", ValueTypes.Bool, ConfigurationScopes.Security,
            "Enable or disable multi-factor authentication."),
        new("security.mfa.code_expiry_seconds", ValueTypes.Int, ConfigurationScopes.Security,
            "MFA code expiration time in seconds."),
        new("branding.logo_url", ValueTypes.String, ConfigurationScopes.Branding,
            "Tenant logo URL used on login screens."),
        new("branding.primary_color", ValueTypes.String, ConfigurationScopes.Branding,
            "Primary accent color for tenant UI."),
        new("branding.login_text", ValueTypes.String, ConfigurationScopes.Branding,
            "Custom login page welcome text."),
        new("notification.email.from_address", ValueTypes.String, ConfigurationScopes.Notification,
            "From address for tenant email notifications.")
    };
}

