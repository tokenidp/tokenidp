namespace IDP.Core.Admin.Roles;

internal class RoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleService> _logger;

    public RoleService(IAppLogger<RoleService> logger,
        ApplicationDbContext dbContext,
        ICache cache)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<Result> CreateRole(CreateUpdateRole request)
    {
        Role appRole = new(
            request.TenantId,
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.Roles.Add(appRole);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<Result> UpdateRole(int id, CreateUpdateRole request)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, CancellationToken.None);

        if (role == null)
        {
            return Result.Failure("NotFound", "Role not found for the Id {0}".FormatString(id));
        }

        role.UpdateRole(
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.Roles.Update(role);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<Result> DeleteRole(int roleId)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            return Result.Failure("NotFound", "Role not found for the Id {0}".FormatString(roleId));
        }

        role.DeleteRole();

        _dbContext.Roles.Update(role);

        var result = await _dbContext.SaveChangesAsync();

        return Result.Success(result);
    }

    public async Task<RoleDto> GetRoleById(int id)
    {
        var role = await _dbContext.Roles
            .Where(u => u.Id == id)
            .Select(RoleDto.Projection)
            .FirstOrDefaultAsync();

        return role;
    }

    public async Task<PaginatedList<RoleSearchDto>> GerRoles(SearchData request)
    {
        var roles = await _dbContext.RolesSearch
           .AsNoTracking()
           .Select(RoleSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        return roles;
    }

    public async Task<IEnumerable<string>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return userRoles;
    }

    public async Task<bool> HasPermission(int userId, string claim)
    {
        _logger.LogDebug("Checking authorization for user {UserId} and claim {Claim}", userId, claim);

        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, claim);

        var hasPermission = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var claimValue = await _dbContext.UserRolePermissions
              .Where(c => c.UserId == userId
                           && c.PermissionType == claim
                           && c.PermissionValue == "true")
              .Select(c => c.PermissionValue)
              .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(claimValue);

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Cache hit for claim authorization {CacheKey}", cacheKey);

        return hasPermission;
    }

    public async Task<bool> HasRole(int userId, string role)
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

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Cache hit for role membership {CacheKey}", cacheKey);

        return hasAssignedRole;
    }
}