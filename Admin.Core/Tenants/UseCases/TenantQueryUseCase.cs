using Admin.Core.Common;

namespace Admin.Core.Tenants.UseCases;

internal sealed class TenantQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TenantQueryUseCase> _logger;

    public TenantQueryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TenantQueryUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<TenantDetail>> GetTenantById(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tenant {TenantId}", tenantId);

        if (_currentUserService.TenantId > 0 && tenantId != _currentUserService.TenantId)
        {
            return ApiResult<TenantDetail>.Failure(
                ApiError.Failure("tenant.forbidden", "Cross-tenant access is not allowed."));
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(TenantDetail.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
            return ApiResult<TenantDetail>.Failure(ApiError.Failure("NotFound",
                "Tenant not found for the Id {0}".FormatString(tenantId)));
        }

        return ApiResult<TenantDetail>.Success(tenant);
    }

    public async Task<ApiResult<PaginatedList<TenantSearchResult>>> GetTenants(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tenants list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var query = _dbContext.Tenants.AsNoTracking();
        if (_currentUserService.TenantId > 0)
        {
            query = query.Where(t => t.Id == _currentUserService.TenantId);
        }

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(tenant =>
                (tenant.TenantName ?? string.Empty).ToLower().Contains(term) ||
                (tenant.TenantCode ?? string.Empty).ToLower().Contains(term) ||
                (tenant.Email ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));
        if (statusCriteria != null)
        {
            criterias = criterias
                .Where(c =>
                    !string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var raw = statusCriteria.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (bool.TryParse(raw, out var isActive))
                {
                    query = query.Where(tenant => tenant.IsActive == isActive);
                }
                else if (string.Equals(raw, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tenant => tenant.IsActive == true);
                }
                else if (string.Equals(raw, "Inactive", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(raw, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(tenant => tenant.IsActive == false);
                }
            }
        }

        var tenants = await query
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .Select(TenantSearchResult.Projection)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} tenants", tenants.TotalCount);

        return ApiResult<PaginatedList<TenantSearchResult>>.Success(tenants);
    }
}
