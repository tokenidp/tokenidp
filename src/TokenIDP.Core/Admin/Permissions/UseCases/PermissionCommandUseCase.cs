using TokenIDP.Domain.AggregateRoots.Permissions;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Permissions.UseCases;

internal class PermissionCommandUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAppLogger<PermissionCommandUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PermissionCommandUseCase(IAppLogger<PermissionCommandUseCase> logger,
        IPermissionRepository permissionRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _permissionRepository = permissionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<int>> CreatePermission(
        CreateUpdatePermission request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating permission {PermissionKey} for tenant {TenantId}",
            request.PermissionKey,
            _currentUserService.TenantId);

        var rawKey = request.PermissionKey ?? string.Empty;
        var rawName = request.PermissionName ?? string.Empty;
        var normalizedKey = rawKey.Trim().ToLowerInvariant();
        var normalizedName = rawName.Trim();
        var validation = Permission.ValidateInput(
            normalizedKey,
            normalizedName,
            request.ControlType);

        if (!validation.IsSuccess)
        {
            return ApiResult<int>.Failure(
                validation.Errors.Select(e =>
                    ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
        }

        var hasDuplicate = await _permissionRepository.PermissionKeyExistsAsync(
            normalizedKey,
            cancellationToken);

        if (hasDuplicate)
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("permission.key.duplicate", "Permission key already exists."));
        }

        var nextSequence = await _permissionRepository.GetNextPermissionSequenceAsync(cancellationToken);

        var permission = new Permission(
            tenantId: _currentUserService.TenantId,
            parentId: request.ParentId,
            sequence: nextSequence,
            permissionKey: normalizedKey,
            permissionName: normalizedName,
            accessUrl: string.IsNullOrWhiteSpace(request.AccessUrl)
                ? null
                : request.AccessUrl.Trim(),
            icon: string.IsNullOrWhiteSpace(request.Icon)
                ? null
                : request.Icon.Trim(),
            controlType: request.ControlType,
            isActive: true
        );

        await _permissionRepository.AddAsync(permission, cancellationToken);

        _logger.LogDebug(
            "Permission created and linked to tenant {TenantId} with Id {PermissionId}",
            _currentUserService.TenantId,
            permission.Id);

        return ApiResult<int>.Success(permission.Id);
    }

    public async Task<ApiResult<int>> UpdatePermission(
        int permissionId,
        CreateUpdatePermission request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating permission {PermissionId}", permissionId);

        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);

        if (permission is null)
        {
            _logger.LogWarning("Permission not found for update: {PermissionId}", permissionId);

            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "permission.not_found",
                    $"Permission not found for Id {permissionId}"));
        }

        if (!string.Equals(permission.PermissionKey, request.PermissionKey,
            StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<int>.Failure(
                ApiError.Failure("permission.key.immutable", "Permission key cannot be changed."));
        }

        var updateResult = permission.Update(
            parentId: request.ParentId,
            sequence: permission.Sequence,
            permissionKey: permission.PermissionKey,
            permissionName: (request.PermissionName ?? string.Empty).Trim(),
            accessUrl: string.IsNullOrWhiteSpace(request.AccessUrl)
                ? null
                : request.AccessUrl.Trim(),
            icon: string.IsNullOrWhiteSpace(request.Icon)
                ? null
                : request.Icon.Trim(),
            controlType: request.ControlType,
            isActive: request.IsActive
        );

        if (!updateResult.IsSuccess)
        {
            return ApiResult<int>.Failure(
                  updateResult.Errors.Select(e => ApiError.Failure($"{e.Code}: {e.Message}")).ToList());
        }

        await _permissionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Permission updated successfully {PermissionId}", permission.Id);

        return ApiResult<int>.Success(permission.Id);
    }

    public async Task<ApiResult<int>> DeletePermission(
        int permissionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting permission {PermissionId}", permissionId);

        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);

        if (permission is null)
        {
            _logger.LogWarning("Permission not found for delete: {PermissionId}", permissionId);

            return ApiResult<int>.Failure(
                ApiError.Failure(
                    "permission.not_found",
                    $"Permission not found for Id {permissionId}"));
        }

        await _permissionRepository.DeleteAsync(permission, cancellationToken);

        _logger.LogDebug("Permission deleted successfully {PermissionId}", permission.Id);

        return ApiResult<int>.Success(permission.Id);
    }
}

