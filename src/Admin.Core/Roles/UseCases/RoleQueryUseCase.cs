using Admin.Core.Common;

namespace Admin.Core.Roles.UseCases;

internal class RoleQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<RoleQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RoleQueryUseCase(IAppLogger<RoleQueryUseCase> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<RoleInfo>> GetRoleById(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching role {RoleId}", id);

        var roleDto = await _dbContext.Roles
            .AsNoTracking()
            .Where(r =>
                r.Id == id &&
                r.TenantId == _currentUserService.TenantId &&
                !r.IsDeleted)
            .Select(RoleInfo.Projection)
            .FirstOrDefaultAsync(cancellationToken);

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

        var roles = await _dbContext.RolesSearch
           .AsNoTracking()
           .Select(RoleList.Projection)
           .ApplyFilter(request.SearchCriterias)
           .ApplySort(request.SortColumn, request.SortOrder)
           .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} roles", roles.TotalCount);

        return ApiResult<PaginatedList<RoleList>>.Success(roles);
    }
}