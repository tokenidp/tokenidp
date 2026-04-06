using TokenIDP.Core.Admin.Tenants;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface ITenantProvisioningService
{
    Task<Tenant> CreateSystemTenantAsync(IApplicationDbContext db, CreateUpdateTenant command, CancellationToken ct);
    Task<Tenant?> ExistsAsync(IApplicationDbContext db, string tenantCode, CancellationToken ct);
}

