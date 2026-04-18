using TokenIDP.Core.Foundation.Contracts;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Roles;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<string>> GetUserRoles(int userId);

    Task<ApiResult<bool>> HasPermission(int userId, string claim);

    Task<ApiResult<bool>> HasRole(int userId, string role);
    Task<Role?> GetRoleAggregateAsync(int roleId, int tenantId, CancellationToken ct);
    Task<RoleInfo?> GetRoleDetailAsync(int tenantId, int roleId, CancellationToken ct);
    Task<PaginatedList<RoleList>> SearchRolesAsync(int tenantId, SearchData request, CancellationToken ct);
    Task<IReadOnlyList<RoleUserCountItem>> GetRoleUserCountsAsync(
        int tenantId,
        IReadOnlyCollection<int> roleIds,
        CancellationToken ct);
    Task<bool> RoleNameExistsAsync(int tenantId, string roleName, int? excludeRoleId, CancellationToken ct);
    Task<RoleAssignmentValidation?> GetRoleAssignmentValidationAsync(int tenantId, int roleId, CancellationToken ct);
    Task<int> AddAsync(Role role, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}

