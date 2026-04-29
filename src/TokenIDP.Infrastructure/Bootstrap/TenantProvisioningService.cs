using TokenIDP.Core.Admin.Tenants;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class TenantProvisioningService : ITenantProvisioningService
{
    public async Task<Tenant> CreateSystemTenantAsync(ApplicationDbContext db,
        CreateUpdateTenant command,
        CancellationToken ct)
    {
        var authSettings = TenantAuthSetting.Create(0);

        authSettings.SetAuthenticationMode(command.AuthSettings.AuthenticationMode);

        if (command.AuthSettings.AllowLocalLogin) authSettings.EnableLocalLogin();
        else authSettings.DisableLocalLogin();

        if (command.AuthSettings.RequireEmailVerification) authSettings.RequireVerifiedEmail();
        else authSettings.AllowUnverifiedEmail();

        if (command.AuthSettings.AllowSelfRegistration) authSettings.EnableSelfRegistration();
        else authSettings.DisableSelfRegistration();

        if (command.AuthSettings.TwoFactorEnabled)
            authSettings.EnableTwoFactor(TimeSpan.FromMinutes(command.AuthSettings.TwoFactorCodeExpiry ?? 300));
        else
            authSettings.DisableTwoFactor();

        var uiSetting = TenantUISetting.Create("Light", "default", "default", "en", string.Empty);

        var createResult = Tenant.Create(
            command.TenantName,
            "system",
            command.Email?.Trim(),
            command.IsActive,
            authSettings,
            uiSetting,
            true,
            out var tenant);

        tenant?.GenerateTenantCode(1);

        db.Tenants.Add(tenant!);

        await db.SaveChangesAsync(ct);

        return tenant!;
    }

    public async Task<Tenant?> FindSystemTenantAsync(
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var existingTenant = await db.Tenants
            .Where(t => !t.IsDeleted &&
                        (t.IsSystemTenant ||
                         t.TenantKey == "system" ||
                         t.TenantName == "system"))
            .OrderByDescending(t => t.IsSystemTenant)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        return existingTenant;
    }
}

