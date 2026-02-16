using Admin.Core.Bootstrap;
using Admin.Core.Tenants;

namespace IDP.Infrastructure.Bootstrap;

internal class TenantProvisioningService : ITenantProvisioningService
{
    public async Task<Tenant> CreateSystemTenantAsync(IApplicationDbContext db, 
        CreateUpdateTenant command, 
        CancellationToken ct)
    {
        var createResult = Tenant.Create(
            command.TenantName,
            command.TenantCode,
            command.Email?.Trim(),
            command.Theme?.Trim(),
            command.LogoUrl?.Trim(),
            command.PrimaryColor?.Trim(),
            command.DefaultLanguage?.Trim(),
            command.LoginText?.Trim(),
            command.TwoFactorEnabled,
            command.TwoFactorCodeExpiry,
            command.HomePageUrl?.Trim(),
            command.IsActive,
            command.TenantType,
            command.SubscriptionType,
            command.AuthenticationMode,
            out var tenant);

        db.Tenants.Add(tenant!);

        await db.SaveChangesAsync(ct);

        return tenant!;
    }

    public async Task<Tenant?> ExistsAsync(IApplicationDbContext db, 
        string tenantCode, 
        CancellationToken ct)
    {
        var existingTenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantCode == tenantCode, ct);

        return existingTenant;
    }
}