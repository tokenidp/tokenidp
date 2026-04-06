using TokenIDP.Core.Admin.Tenants;

namespace TokenIDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultTenants
{
    public static readonly CreateUpdateTenant SystemTenant = new CreateUpdateTenant
    {
        TenantName = "system",
        Email = "admin@system.local",
        IsActive = true,
        AuthSettings = new TenantAuthSettingDetail()
        {
            AuthenticationMode = AuthenticationModes.Local,
            AllowLocalLogin = true,
            TwoFactorEnabled = false,
            TwoFactorCodeExpiry = 300
        }
    };
}
