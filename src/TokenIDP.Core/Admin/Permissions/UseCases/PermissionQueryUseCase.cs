using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Permissions.UseCases;

internal class PermissionQueryUseCase
{

    private readonly IPermissionRepository _permissionRepository;
    private readonly IAppLogger<PermissionQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PermissionQueryUseCase(IAppLogger<PermissionQueryUseCase> logger,
        IPermissionRepository permissionRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _permissionRepository = permissionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<IEnumerable<PermissionList>>> GetPermissions()
    {
        _logger.LogDebug("Fetching permissions list");

        var permissions = (await _permissionRepository.GetActivePermissionsAsync(CancellationToken.None)).ToList();

        _logger.LogDebug("Fetched {Count} roles", permissions.Count);

        return ApiResult<IEnumerable<PermissionList>>.Success(permissions);
    }

    public async Task<ApiResult<PaginatedList<PermissionList>>> GetPermissions(SearchData request)
    {
        _logger.LogDebug("Fetching permissions list");

        var permissions = await _permissionRepository.SearchPermissionsAsync(
            request,
            CancellationToken.None);

        _logger.LogDebug("Fetched {Count} roles", permissions.TotalCount);

        return ApiResult<PaginatedList<PermissionList>>.Success(permissions);
    }

    public async Task<ApiResult<PermissionById>> GetPermissionById(int permissionId)
    {
        _logger.LogDebug("Fetching permission {PermissionId}", permissionId);

        var permission = await _permissionRepository.GetPermissionDetailAsync(
            permissionId,
            CancellationToken.None);

        if (permission is null)
        {
            _logger.LogWarning("Permission not found: {PermissionId}", permissionId);

            return ApiResult<PermissionById>.Failure(
                ApiError.Failure(
                    "permission.not_found",
                    $"Permission not found for Id {permissionId}"));
        }

        return ApiResult<PermissionById>.Success(permission);
    }
}

