using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Roles;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleRepository> _logger;

    public RoleRepository(ApplicationDbContext dbContext,
        IAppLogger<RoleRepository> logger,
        ICache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return userRoles;
    }

    public async Task<ApiResult<bool>> HasPermission(int userId, string permission)
    {
        _logger.LogDebug("Checking authorization for user {UserId} and claim {Claim}", userId, permission);

        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, permission);

        var hasPermission = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var claimValue = await _dbContext.UserRolePermissions
              .Where(c => c.UserId == userId
                           && c.Permissionkey == permission
                           && c.IsAllowed)
              .Select(c => c.IsAllowed)
              .FirstOrDefaultAsync();

            return claimValue;

        }, new TimeSpan(0, 60, 0));

        _logger.LogDebug("Cache hit for claim authorization {CacheKey}", cacheKey);

        return ApiResult<bool>.Success(hasPermission);
    }

    public async Task<ApiResult<bool>> HasRole(int userId, string role)
    {
        _logger.LogDebug("Checking role membership for user {UserId} and role {Role}", userId, role);

        var cacheKey = CacheKeys.USER_ROLE.FormatCacheKey(userId, role);

        var hasAssignedRole = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {

            var assignedRole = await (from ur in _dbContext.UserRoles
                                      join r in _dbContext.Roles on ur.RoleId equals r.Id
                                      where ur.UserId == userId
                                        && r.Name == role
                                        && r.IsDeleted != true
                                        && r.IsActive != false
                                      select r.Name).FirstOrDefaultAsync();

            _logger.LogDebug("Cached role membership for {CacheKey}", cacheKey);

            return !string.IsNullOrEmpty(assignedRole);

        }, new TimeSpan(0, 60, 0));

        _logger.LogDebug("Cache hit for role membership {CacheKey}", cacheKey);

        return ApiResult<bool>.Success(hasAssignedRole);
    }

    public Task<Role?> GetRoleAggregateAsync(int roleId, int tenantId, CancellationToken ct)
    {
        return _dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(
                r => r.Id == roleId &&
                     r.TenantId == tenantId &&
                     !r.IsDeleted,
                ct);
    }

    public Task<RoleInfo?> GetRoleDetailAsync(int tenantId, int roleId, CancellationToken ct)
    {
        return _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId && r.TenantId == tenantId && !r.IsDeleted)
            .Select(RoleInfo.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public Task<PaginatedList<RoleList>> SearchRolesAsync(int tenantId, SearchData request, CancellationToken ct)
    {
        return _dbContext.RolesSearch
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Select(RoleList.Projection)
            .ApplyFilter(request.SearchCriterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public Task<bool> RoleNameExistsAsync(int tenantId, string roleName, int? excludeRoleId, CancellationToken ct)
    {
        var normalized = roleName.ToLowerInvariant();

        return _dbContext.Roles.AnyAsync(
            r => r.TenantId == tenantId &&
                 (!excludeRoleId.HasValue || r.Id != excludeRoleId.Value) &&
                 r.Name.ToLower() == normalized,
            ct);
    }

    public async Task<RoleAssignmentValidation?> GetRoleAssignmentValidationAsync(int tenantId, int roleId, CancellationToken ct)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId && r.TenantId == tenantId && !r.IsDeleted)
            .Select(r => new RoleAssignmentValidation
            {
                Exists = true,
                IsActive = r.IsActive,
                IsAssignableToNewUsers = r.IsAssignableToNewUsers
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> AddAsync(Role role, CancellationToken ct)
    {
        _dbContext.Roles.Add(role);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}


