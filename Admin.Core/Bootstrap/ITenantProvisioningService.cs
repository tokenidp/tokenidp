using Admin.Core.Tenants;

namespace Admin.Core.Bootstrap;

public interface ITenantProvisioningService
{
    Task<Tenant> CreateSystemTenantAsync(IApplicationDbContext db, CreateUpdateTenant command, CancellationToken ct);
    Task<Tenant?> ExistsAsync(IApplicationDbContext db, string tenantCode, CancellationToken ct);
}
