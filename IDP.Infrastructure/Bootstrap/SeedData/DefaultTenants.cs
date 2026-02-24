using Admin.Core.Tenants;

namespace IDP.Infrastructure.Bootstrap.SeedData;

internal class DefaultTenants
{
    public static readonly TenantDetail SystemTenant = new TenantDetail
    {
        TenantName = "system",
        TenantCode= "System001",
        Email = "admin@system.local",        
        IsActive =  true,
        AuthSettings = new TenantAuthSettingDetail()
        {
            AuthenticationMode = AuthenticationModes.Local,
            AllowLocalLogin = true,
            TwoFactorEnabled = false,
            TwoFactorCodeExpiry = 300
        }
    };
}