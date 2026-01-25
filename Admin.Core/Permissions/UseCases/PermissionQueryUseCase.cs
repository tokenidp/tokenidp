using Admin.Core.Common;

namespace Admin.Core.Permissions.UseCases;

internal class PermissionQueryUseCase
{

    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<PermissionQueryUseCase> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PermissionQueryUseCase(IAppLogger<PermissionQueryUseCase> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<IEnumerable<PermissionList>>> GetPermissions()
    {
        _logger.LogDebug("Fetching permissions list");

        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive != false)
            .OrderBy(p => p.Sequence)
            .ThenBy(p => p.PermissionKey)
            .Select(PermissionList.Projection)
            .ToListAsync();

        _logger.LogDebug("Fetched {Count} roles", permissions.Count);

        return ApiResult<IEnumerable<PermissionList>>.Success(permissions);
    }

    public async Task<ApiResult<PaginatedList<PermissionList>>> GetPermissions(SearchData request)
    {
        _logger.LogDebug("Fetching permissions list");

        var query = _dbContext.Permissions.AsNoTracking();
        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();

        var controlTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "ControlType", StringComparison.OrdinalIgnoreCase));

        if (controlTypeCriteria != null &&
            Enum.TryParse<ControlTypes>(controlTypeCriteria.Value, true, out var controlType))
        {
            query = query.Where(p => p.ControlType == controlType);
        }

        var activeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Active", StringComparison.OrdinalIgnoreCase));

        if (activeCriteria != null)
        {
            var raw = activeCriteria.Value?.Trim();
            if (string.Equals(raw, "Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.IsActive);
            }
            else if (string.Equals(raw, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => !p.IsActive);
            }
        }

        criterias = criterias
            .Where(c =>
                !string.Equals(c.ColumnName, "ControlType", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var permissions = await query
            .Select(PermissionList.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} roles", permissions.TotalCount);

        return ApiResult<PaginatedList<PermissionList>>.Success(permissions);
    }

    public async Task<ApiResult<PermissionById>> GetPermissionById(int permissionId)
    {
        _logger.LogDebug("Fetching permission {PermissionId}", permissionId);

        var permission = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.Id == permissionId)
            .Select(PermissionById.Projection)
            .FirstOrDefaultAsync(CancellationToken.None);

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
