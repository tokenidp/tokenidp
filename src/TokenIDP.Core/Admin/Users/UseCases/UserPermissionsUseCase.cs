using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Users.UseCases;

internal class UserPermissionsUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IAppLogger<UserPermissionsUseCase> _logger;

    public UserPermissionsUseCase(ICurrentUserService currentUserService,
        ITenantContextAccessor tenantContextAccessor,
        IAppLogger<UserPermissionsUseCase> logger,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<ApiResult<UserPermission>> GetUserPermissions()
    {
        _logger.LogDebug("Fetching user info for: {UserId}", _currentUserService.UserId);

        var user = await _userRepository.GetUserById(_currentUserService.UserId);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "User not found."));
        }

        _logger.LogDebug("Found user {UserName} (Tenant: {TenantId})",
            user.UserName ?? string.Empty, user.TenantId);

        var permissions = await _userRepository.GetUserPermissionsAsync(
            _currentUserService.UserId,
            CancellationToken.None);
        if (!_tenantContextAccessor.IsSystemTenant)
        {
            permissions = permissions
                .Where(permission =>
                    !permission.PermissionKey.StartsWith("tenants.", StringComparison.OrdinalIgnoreCase) &&
                    !permission.PermissionKey.Equals("tenant.secret.reveal", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!permissions.IsSafe())
        {
            _logger.LogWarning("No claims found for user {UserId}", _currentUserService.UserId);
            return ApiResult<UserPermission>.Failure(ApiError.Failure("NotFound", "Claims not found."));
        }

        _logger.LogDebug("Found {ClaimCount} claims for user {UserId}",
            permissions.Count, _currentUserService.UserId);

        var userInfo = UserPermission.Create(
            user.Id,
            user.TenantId,
            _tenantContextAccessor.TenantKey,
            _tenantContextAccessor.IsSystemTenant,
            user.FullName,
            permissions);

        _logger.LogInfo("Successfully compiled user info for {UserId}", _currentUserService.UserId);

        return ApiResult<UserPermission>.Success(userInfo);
    }
}


