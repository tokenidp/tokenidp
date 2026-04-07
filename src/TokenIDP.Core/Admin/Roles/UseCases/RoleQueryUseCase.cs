using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Roles.UseCases;

internal class RoleQueryUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAppLogger<RoleQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RoleQueryUseCase(IAppLogger<RoleQueryUseCase> logger,
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
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
}
