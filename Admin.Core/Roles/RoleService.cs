using IDP.Domain.AggregateRoots;

namespace Admin.Core.Roles;

internal class RoleService
{
    private static readonly System.Text.RegularExpressions.Regex PermissionKeyRegex =
        new("^[A-Z0-9]+([._][A-Z0-9]+)*$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<RoleService> _logger;

    public RoleService(IAppLogger<RoleService> logger,
        IApplicationDbContext dbContext,
        ICache cache)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<ApiResult<int>> CreateRole(CreateUpdateRole request)
    {
        _logger.LogDebug("Creating role for tenant {TenantId}", request.TenantId);

        Role appRole = new(
            request.TenantId,
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.Roles.Add(appRole);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Role created with Id {RoleId}", appRole.Id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> UpdateRole(int id, CreateUpdateRole request)
    {
        _logger.LogDebug("Updating role {RoleId}", id);

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, CancellationToken.None);

        if (role == null)
        {
            _logger.LogWarning("Role not found for update: {RoleId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Role not found for the Id {0}".FormatString(id)));
        }

        role.UpdateRole(
            request.Name,
            request.RoleDescription,
            request.IsActive
            );

        _dbContext.Roles.Update(role);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Role updated {RoleId}", id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> DeleteRole(int roleId)
    {
        _logger.LogDebug("Deleting role {RoleId}", roleId);

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            _logger.LogWarning("Role not found for delete: {RoleId}", roleId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Role not found for the Id {0}".FormatString(roleId)));
        }

        role.DeleteRole();

        _dbContext.Roles.Update(role);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Role deleted {RoleId}", roleId);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<RoleDto>> GetRoleById(int id)
    {
        _logger.LogDebug("Fetching role {RoleId}", id);

        var role = await _dbContext.Roles
            .Where(u => u.Id == id)
            .Select(RoleDto.Projection)
            .FirstOrDefaultAsync();

        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", id);
            return ApiResult<RoleDto>.Failure(ApiError.Failure("NotFound",
                "Role not found for the Id {0}".FormatString(id)));
        }

        return ApiResult<RoleDto>.Success(role);
    }

    public async Task<ApiResult<PaginatedList<RoleSearchDto>>> GerRoles(SearchData request)
    {
        _logger.LogDebug("Fetching roles list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var roles = await _dbContext.RolesSearch
           .AsNoTracking()
           .Select(RoleSearchDto.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} roles", roles.TotalCount);

        return ApiResult<PaginatedList<RoleSearchDto>>.Success(roles);
    }

    public async Task<ApiResult<IEnumerable<string>>> GetUserRoles(int userId)
    {
        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && r.IsDeleted != true && r.IsActive != false
                               select r.Name).ToListAsync();

        return ApiResult<IEnumerable<string>>.Success(userRoles);
    }

    public async Task<ApiResult<bool>> HasPermission(int userId, string claim)
    {
        _logger.LogDebug("Checking authorization for user {UserId} and claim {Claim}", userId, claim);

        var cacheKey = CacheKeys.USER_CLAIM.FormatCacheKey(userId, claim);

        var hasPermission = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var claimValue = await _dbContext.UserRolePermissions
              .Where(c => c.UserId == userId
                           && c.Permissionkey == claim
                           && c.PermissionValue == "true")
              .Select(c => c.PermissionValue)
              .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(claimValue);

        }, new TimeSpan(0, 15, 0));

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

        }, new TimeSpan(0, 15, 0));

        _logger.LogDebug("Cache hit for role membership {CacheKey}", cacheKey);

        return ApiResult<bool>.Success(hasAssignedRole);
    }

    public async Task<ApiResult<IEnumerable<PermissionListDto>>> GetPermissions()
    {
        _logger.LogDebug("Fetching permissions list");

        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive != false)
            .OrderBy(p => p.Sequence)
            .Select(PermissionListDto.Projection)
            .ToListAsync();

        return ApiResult<IEnumerable<PermissionListDto>>.Success(permissions);
    }

    public async Task<ApiResult<IEnumerable<PermissionParentDto>>> GetParentPermissions()
    {
        _logger.LogDebug("Fetching permission parents");

        var parents = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive != false
                        && (p.ControlType == "Link" || p.ControlType == "link"))
            .OrderBy(p => p.Sequence)
            .Select(PermissionParentDto.Projection)
            .ToListAsync();

        return ApiResult<IEnumerable<PermissionParentDto>>.Success(parents);
    }

    public async Task<ApiResult<PermissionCreateDto>> CreatePermission(CreatePermissionRequest request)
    {
        if (request == null)
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Request payload is required."));
        }

        var permissionName = request.PermissionName?.Trim();
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Permission name is required."));
        }

        var permissionKey = request.PermissionKey?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Permission key is required."));
        }

        if (!PermissionKeyRegex.IsMatch(permissionKey))
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation",
                    "Permission key must use uppercase letters with underscores or dots."));
        }

        var controlType = request.ControlType?.Trim();
        var isLink = string.Equals(controlType, "Link", StringComparison.OrdinalIgnoreCase);
        var isAction = string.Equals(controlType, "Action", StringComparison.OrdinalIgnoreCase);

        if (!isLink && !isAction)
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "ControlType must be Link or Action."));
        }

        if (isAction && request.ParentId == null)
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Parent permission is required for actions."));
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _dbContext.Permissions
                .Where(p => p.Id == request.ParentId
                            && p.IsActive != false)
                .Select(p => new { p.Id, p.ControlType })
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                return ApiResult<PermissionCreateDto>.Failure(
                    ApiError.Failure("Validation", "Parent permission not found."));
            }

            if (!string.Equals(parent.ControlType, "Link", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<PermissionCreateDto>.Failure(
                    ApiError.Failure("Validation", "Parent permission must be a menu link."));
            }
        }

        var accessUrl = request.AccessUrl?.Trim();
        if (isLink)
        {
            if (string.IsNullOrWhiteSpace(accessUrl))
            {
                return ApiResult<PermissionCreateDto>.Failure(
                    ApiError.Failure("Validation", "Access URL is required for menu links."));
            }

            if (!accessUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return ApiResult<PermissionCreateDto>.Failure(
                    ApiError.Failure("Validation", "Access URL must start with '/'."));
            }
        }
        else if (!string.IsNullOrWhiteSpace(accessUrl))
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Access URL is not valid for actions."));
        }

        var exists = await _dbContext.Permissions
            .AnyAsync(p => p.Permissionkey == permissionKey && p.IsActive != false);

        if (exists)
        {
            return ApiResult<PermissionCreateDto>.Failure(
                ApiError.Failure("Validation", "Permission key already exists."));
        }

        var maxSequence = await _dbContext.Permissions
            .Where(p => p.ParentId == request.ParentId && p.IsActive != false)
            .Select(p => (int?)p.Sequence)
            .MaxAsync();

        var sequence = request.Sequence.HasValue && request.Sequence.Value > 0
            ? request.Sequence.Value
            : (maxSequence ?? 0) + 1;

        var permission = new Permission(request.ParentId,
            permissionKey,
            permissionName,
            isLink ? accessUrl : null,
            isLink ? "Link" : "Action",
            true,
            request.IsActive);

        _dbContext.Permissions.Add(permission);

        await _dbContext.SaveChangesAsync();

        var result = new PermissionCreateDto
        {
            Id = permission.Id,
            ParentId = permission.ParentId,
            Sequence = permission.Sequence,
            PermissionKey = permission.Permissionkey,
            PermissionName = permission.PermissionName,
            AccessUrl = permission.AccessUrl,
            ControlType = permission.ControlType,
            Icon = permission.Icon,
            IsActive = permission.IsActive
        };

        return ApiResult<PermissionCreateDto>.Success(result);
    }

    public async Task<ApiResult<int>> UpdateRolePermissions(int roleId, RolePermissionsUpdateRequest request)
    {
        if (request.PermissionIds == null)
        {
            return ApiResult<int>.Failure(ApiError.Failure("Validation",
                "PermissionIds are required."));
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            _logger.LogWarning("Role not found for permissions update: {RoleId}", roleId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Role not found for the Id {0}".FormatString(roleId)));
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();

        var activePermissions = await _dbContext.Permissions
            .Where(p => permissionIds.Contains(p.Id) && p.IsActive != false)
            .Select(p => p.Id)
            .ToListAsync();

        if (activePermissions.Count != permissionIds.Length)
        {
            return ApiResult<int>.Failure(ApiError.Failure("Validation",
                "One or more permissions are invalid or inactive."));
        }

        var tenantPermissions = await _dbContext.TenantPermissions
            .Where(tp => tp.TenantId == role.TenantId && permissionIds.Contains(tp.PermissionId))
            .ToListAsync();

        var desiredPermissionIds = tenantPermissions.Select(tp => tp.Id).ToHashSet();

        var existing = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        var toRemove = existing.Where(rp => !desiredPermissionIds.Contains(rp.TenantPermissionId)).ToList();
        var existingIds = existing.Select(rp => rp.TenantPermissionId).ToHashSet();
        var toAdd = tenantPermissions
            .Where(tp => !existingIds.Contains(tp.Id))
            .Select(tp => new RolePermission(tp.Id, roleId, tp.ClaimType, "true"))
            .ToList();

        if (toRemove.Count > 0)
        {
            _dbContext.RolePermissions.RemoveRange(toRemove);
        }

        if (toAdd.Count > 0)
        {
            _dbContext.RolePermissions.AddRange(toAdd);
        }

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Role permissions updated {RoleId} ({Count})", roleId, permissionIds.Length);

        return ApiResult<int>.Success(result);
    }
}