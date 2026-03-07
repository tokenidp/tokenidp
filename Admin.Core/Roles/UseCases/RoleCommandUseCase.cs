namespace Admin.Core.Roles.UseCases;

internal class RoleCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<RoleCommandUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RoleCommandUseCase(IAppLogger<RoleCommandUseCase> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<int>> CreateRole(
        CreateUpdateRole request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating role for tenant {TenantId}", _currentUserService.TenantId);

        var roleName = request.RoleName?.Trim() ?? string.Empty;
        var roleDescription = request.RoleDescription?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("role.name.invalid", "Role name cannot be empty."));
        }

        var isActive = request.IsActive ?? true;
        var isAssignableToExternalUsers = request.IsAssignableToExternalUsers;
        if (isAssignableToExternalUsers && !isActive)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.external_assignable.invalid",
                    "Only active roles can be assignable to external users."));
        }

        if (IsReservedSystemRole(roleName) && isAssignableToExternalUsers)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.external_assignable.invalid",
                    "System roles cannot be assignable to external users."));
        }

        var roleNameLower = roleName.ToLowerInvariant();
        var roleExists = await _dbContext.Roles
            .AnyAsync(
                r => r.TenantId == _currentUserService.TenantId &&
                     !r.IsDeleted &&
                     r.Name.ToLower() == roleNameLower,
                cancellationToken);

        if (roleExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("role.name.duplicate", "Role name already exists."));
        }

        var role = new Role(
            tenantId: _currentUserService.TenantId,
            name: roleName,
            description: roleDescription,
            isActive: isActive,
            isAssignableToExternalUsers: isAssignableToExternalUsers
        );

        var permissions = request.RolePermissions ?? new List<CreateUpdateRolePermission>();
        foreach (var permission in permissions)
        {
            var permissionResult = role.AddPermission(
                tenantPermissionId: permission.PermissionId,
                permissionKey: permission.PermissionKey,
                isAllowed: permission.IsAllowed
            );

            if (!permissionResult.IsSuccess)
            {
                return ApiResult<int>.Failure(
                    permissionResult.Errors.Select(e => ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
            }
        }

        _dbContext.Roles.Add(role);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Role created successfully with Id {RoleId} in tenant {TenantId}",
            role.Id, _currentUserService.TenantId);

        return ApiResult<int>.Success(role.Id);
    }

    public async Task<ApiResult<int>> UpdateRole(
        int id,
        CreateUpdateRole request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating role with Id {RoleId} in tenant {TenantId}",
            id, _currentUserService.TenantId);

        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(
                r => r.Id == id &&
                     r.TenantId == _currentUserService.TenantId &&
                     !r.IsDeleted,
                cancellationToken);

        if (role is null)
        {
            _logger.LogWarning("Role not found for update: {RoleId}", id);

            return ApiResult<int>.Failure(
                ApiError.Failure("role.not_found", $"Role not found for the Id {id}"));
        }

        var roleName = request.RoleName?.Trim() ?? string.Empty;
        var roleDescription = request.RoleDescription?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("role.name.invalid", "Role name cannot be empty."));
        }

        var isActive = request.IsActive ?? role.IsActive;
        var isAssignableToExternalUsers = request.IsAssignableToExternalUsers;
        if (isAssignableToExternalUsers && !isActive)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.external_assignable.invalid",
                    "Only active roles can be assignable to external users."));
        }

        if (!role.IsEditable && isAssignableToExternalUsers != role.IsAssignableToExternalUsers)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.external_assignable.not_editable",
                    "System roles cannot modify external user assignment."));
        }

        if (IsReservedSystemRole(roleName) && isAssignableToExternalUsers)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.external_assignable.invalid",
                    "System roles cannot be assignable to external users."));
        }

        var roleNameLower = roleName.ToLowerInvariant();
        var roleExists = await _dbContext.Roles
            .AnyAsync(
                r => r.TenantId == _currentUserService.TenantId &&
                     !r.IsDeleted &&
                     r.Id != role.Id &&
                     r.Name.ToLower() == roleNameLower,
                cancellationToken);

        if (roleExists)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("role.name.duplicate", "Role name already exists."));
        }

        var updateResult = role.Update(
            name: roleName,
            description: roleDescription,
            isActive: isActive,
            isAssignableToExternalUsers: isAssignableToExternalUsers
        );

        if (!updateResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                updateResult.Errors.Select(e => ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
        }

        var permissions = request.RolePermissions ?? new List<CreateUpdateRolePermission>();
        foreach (var permission in permissions)
        {

            var existingPermission = role.RolePermissions
                 .FirstOrDefault(p => p.PermissionKey == permission.PermissionKey);

            Result permissionResult;

            if (existingPermission is null)
            {
                permissionResult = role.AddPermission(
                    tenantPermissionId: permission.PermissionId,
                    permissionKey: permission.PermissionKey,
                    isAllowed: permission.IsAllowed);
            }
            else
            {
                permissionResult = role.UpdatePermission(
                    permissionKey: permission.PermissionKey,
                    isAllowed: permission.IsAllowed);
            }

            if (!permissionResult.IsSuccess)
            {
                return ApiResult<int>.Failure(
                    permissionResult.Errors.Select(e => ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Role updated successfully with Id {RoleId} in tenant {TenantId}",
            role.Id, _currentUserService.TenantId);

        return ApiResult<int>.Success(role.Id);
    }

    public async Task<ApiResult<int>> DeleteRole(int roleId)
    {
        _logger.LogDebug("Deleting role with Id {RoleId} in tenant {TenantId}",
            roleId, _currentUserService.TenantId);

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, CancellationToken.None);

        if (role is null)
        {
            _logger.LogWarning("Role not found for delete: {RoleId}", roleId);

            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "role.not_found",
                    $"Role not found for the Id {roleId}"));
        }

        var deleteResult = role.Delete();

        if (!deleteResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                   deleteResult.Errors.Select(e => ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Role deleted successfully with Id {RoleId} in tenant {TenantId}",
            role.Id, _currentUserService.TenantId);

        return ApiResult<int>.Success(role.Id);
    }

    private static bool IsReservedSystemRole(string? roleName)
    {
        var normalized = (roleName ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "admin" or "administrator" or "owner";
    }
}