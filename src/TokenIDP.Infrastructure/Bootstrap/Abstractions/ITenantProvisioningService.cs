using TokenIDP.Core.Admin.Tenants;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface ITenantProvisioningService
{
    Task<Tenant> CreateSystemTenantAsync(ApplicationDbContext db, CreateUpdateTenant command, CancellationToken ct);
    Task<Tenant?> FindSystemTenantAsync(ApplicationDbContext db, CancellationToken ct);
}

