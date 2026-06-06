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

        var uiSettingsRequest = command.UISetting ?? new TenantUISettingDetail();
        var uiSetting = TenantUISetting.Create(
            uiSettingsRequest.Theme ?? "Light",
            uiSettingsRequest.LogoUrl ?? string.Empty,
            uiSettingsRequest.PrimaryColor ?? "default",
            uiSettingsRequest.DefaultLanguage ?? "en",
            uiSettingsRequest.LoginText ?? string.Empty);

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

        tenant!.MarkProvisioned();
        await db.SaveChangesAsync(ct);

        return tenant!;
    }

    public async Task<Tenant?> FindSystemTenantAsync(
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var existingTenant = await db.Tenants
            .Include(t => t.TenantUISetting)
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

