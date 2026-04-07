using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Domain.AggregateRoots.Permissions;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int permissionId, CancellationToken ct);
    Task<IEnumerable<PermissionList>> GetActivePermissionsAsync(CancellationToken ct);
    Task<PaginatedList<PermissionList>> SearchPermissionsAsync(SearchData request, CancellationToken ct);
    Task<PermissionById?> GetPermissionDetailAsync(int permissionId, CancellationToken ct);
    Task<PermissionLookups> GetPermissionLookupsAsync(int tenantId, CancellationToken ct);
    Task<bool> PermissionKeyExistsAsync(string permissionKey, CancellationToken ct);
    Task<int> GetNextPermissionSequenceAsync(CancellationToken ct);
    Task<int> AddAsync(Permission permission, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
