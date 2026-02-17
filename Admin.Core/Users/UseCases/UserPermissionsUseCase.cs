using Admin.Core.Permissions;
using IDP.Foundation.Abstractions.Stores;

namespace Admin.Core.Users.UseCases;

internal class UserPermissionsUseCase
{
    private readonly IIdentityStore _identityStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<UserPermissionsUseCase> _logger;

    public UserPermissionsUseCase(ICurrentUserService currentUserService,
        IApplicationDbContext applicationDbContext,
        IAppLogger<UserPermissionsUseCase> logger,
        IIdentityStore identityStore)
    {
        _currentUserService = currentUserService;
        _dbContext = applicationDbContext;
        _logger = logger;
        _identityStore = identityStore;
    }

    public async Task<ApiResult<UserPermission>> GetUserPermissions()
    {
        _logger.LogDebug("Fetching user info for: {UserId}", _currentUserService.UserId);

        var user = await _identityStore.GetUserById(_currentUserService.UserId);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "User not found."));
        }

        _logger.LogDebug("Found user {UserName} (Tenant: {TenantId})",
            user.UserName ?? string.Empty, user.TenantId);

        var permissions = await _dbContext.UserRolePermissions
            .Where(c => c.UserId == _currentUserService.UserId)
            .Select(c => new PermissionInfo(
                c.Id,
                c.ParentId,
                c.UserId,
                c.Sequence,
                c.PermissionName,
                c.IsAllowed ? "true" : "false",
                c.Permissionkey,
                c.Icon,
                c.AccessUrl,
                c.RoleName,
                c.ControlType))
            .ToListAsync();

        if (!permissions.IsSafe())
        {
            _logger.LogWarning("No claims found for user {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "Claims not found."));
        }

        _logger.LogDebug("Found {ClaimCount} claims for user {UserId}",
            permissions.Count, _currentUserService.UserId);

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for user {UserId}", _currentUserService.UserId);
        }

        var userInfo = UserPermission.Create(
            user.Id,
            user.TenantId,
            user.FullName,
            tenant?.HomePageUrl ?? string.Empty,
            permissions);

        _logger.LogInfo("Successfully compiled user info for {UserId}", _currentUserService.UserId);

        return ApiResult<UserPermission>.Success(userInfo);
    }
}
