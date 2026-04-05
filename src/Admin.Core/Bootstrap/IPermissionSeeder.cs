using Admin.Core.Permissions;
using IDP.Domain.AggregateRoots.Permissions;

namespace Admin.Core.Bootstrap;

public interface IPermissionSeeder
{
    Task<bool> ExistsAsync(IApplicationDbContext db, int tenantId, string permissionKey, CancellationToken ct);
    Task<Permission> CreateAsync(IApplicationDbContext db, int tenantId, CreateUpdatePermission command, CancellationToken ct);
}