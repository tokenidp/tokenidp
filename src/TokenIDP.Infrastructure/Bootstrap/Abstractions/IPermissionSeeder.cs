using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Infrastructure.Persistence;

namespace TokenIDP.Infrastructure.Bootstrap;

public interface IPermissionSeeder
{
    Task<bool> ExistsAsync(ApplicationDbContext db, int tenantId, string permissionKey, CancellationToken ct);
    Task<Permission> CreateAsync(ApplicationDbContext db, int tenantId, CreateUpdatePermission command, CancellationToken ct);
}
