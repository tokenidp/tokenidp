using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Users;

namespace TokenIDP.Core.Admin.Roles.UseCases;

internal class RoleQueryUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppLogger<RoleQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RoleQueryUseCase(IAppLogger<RoleQueryUseCase> logger,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<RoleInfo>> GetRoleById(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching role {RoleId}", id);

        var roleDto = await _roleRepository.GetRoleDetailAsync(
            _currentUserService.TenantId,
            id,
            cancellationToken);

        if (roleDto is null)
        {
            _logger.LogWarning("Role not found: {RoleId}", id);

            return ApiResult<RoleInfo>.Failure(
                ApiError.Failure(
                    "role.not_found",
                    $"Role not found for the Id {id}"));
        }

        return ApiResult<RoleInfo>.Success(roleDto);
    }

    public async Task<ApiResult<PaginatedList<RoleList>>> GerRoles(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching roles list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var roles = await _roleRepository.SearchRolesAsync(
            _currentUserService.TenantId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} roles", roles.TotalCount);

        return ApiResult<PaginatedList<RoleList>>.Success(roles);
    }

    public async Task<ApiResult<IReadOnlyList<RoleUserCountItem>>> GetRoleUserCounts(
        RoleUserCountRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleIds = (request?.RoleIds ?? Array.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
        {
            return ApiResult<IReadOnlyList<RoleUserCountItem>>.Success(Array.Empty<RoleUserCountItem>());
        }

        _logger.LogDebug("Fetching user counts for {Count} roles", roleIds.Length);

        var counts = await _roleRepository.GetRoleUserCountsAsync(
            _currentUserService.TenantId,
            roleIds,
            cancellationToken);

        return ApiResult<IReadOnlyList<RoleUserCountItem>>.Success(counts);
    }

    public async Task<ApiResult<PaginatedList<UserSearchResult>>> GetUsersByRole(
        int roleId,
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching users for role {RoleId}. Page {PageNumber} Size {PageSize}",
            roleId,
            request.PageNumber,
            request.PageSize);

        var role = await _roleRepository.GetRoleDetailAsync(
            _currentUserService.TenantId,
            roleId,
            cancellationToken);

        if (role is null)
        {
            _logger.LogWarning("Role not found for user listing: {RoleId}", roleId);

            return ApiResult<PaginatedList<UserSearchResult>>.Failure(
                ApiError.Failure(
                    "role.not_found",
                    $"Role not found for the Id {roleId}"));
        }

        var users = await _userRepository.SearchUsersByRoleAsync(
            _currentUserService.TenantId,
            roleId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} users for role {RoleId}", users.TotalCount, roleId);

        return ApiResult<PaginatedList<UserSearchResult>>.Success(users);
    }
}
