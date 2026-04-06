using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Core.Admin.Bootstrap;

public interface IPermissionSeeder
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string permissionKey, CancellationToken ct);
    Task<Permission> CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdatePermission command, CancellationToken ct);
}
