using Admin.Core.Common;
using IDP.Domain.Specifications;
using IDP.Foundation.Extensions;

namespace Admin.Core.Configurations;

internal sealed class GetTenantConfigurationsUseCase
{
    private readonly ITenantConfigurationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<GetTenantConfigurationsUseCase> _logger;

    public GetTenantConfigurationsUseCase(
        ITenantConfigurationRepository repository,
        ICurrentUserService currentUserService,
        IAppLogger<GetTenantConfigurationsUseCase> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<PaginatedList<TenantConfigurationDto>>> GetTenantConfigurations(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        _logger.LogDebug("Fetching configurations for tenant {TenantId}", tenantId);

        if (tenantId <= 0)
        {
            return ApiResult<PaginatedList<TenantConfigurationDto>>.Failure(
                ApiError.Failure("configuration.tenant.invalid", "Tenant context is required."));
        }

        var query = _repository.Query()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, TenantConfigurationConstants.SearchColumn, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.ColumnName, TenantConfigurationConstants.KeyColumn, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.ColumnName, TenantConfigurationConstants.ConfigKeyColumn, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim();
            if (term.Length >= TenantConfigurationConstants.MinSearchLength)
            {
                var normalized = term.ToLowerInvariant();
                query = query.Where(c => c.ConfigKey.ToLower().Contains(normalized));
            }
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, TenantConfigurationConstants.SearchColumn, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c.ColumnName, TenantConfigurationConstants.KeyColumn, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c.ColumnName, TenantConfigurationConstants.ConfigKeyColumn, StringComparison.OrdinalIgnoreCase))
            .Select(c =>
            {
                if (string.Equals(c.ColumnName, TenantConfigurationConstants.ScopeColumn, StringComparison.OrdinalIgnoreCase))
                {
                    return new SearchCriteria
                    {
                        ColumnName = nameof(IDP.Domain.AggregateRoots.Configuration.Scope),
                        Value = c.Value,
                        ColumnType = SearchColumnType.String
                    };
                }

                if (string.Equals(c.ColumnName, "ValueType", StringComparison.OrdinalIgnoreCase))
                {
                    return new SearchCriteria
                    {
                        ColumnName = nameof(IDP.Domain.AggregateRoots.Configuration.ValueType),
                        Value = c.Value,
                        ColumnType = SearchColumnType.String
                    };
                }

                return c;
            })
            .ToList();

        var sortColumn = NormalizeSortColumn(request.SortColumn);

        var configurations = await query
            .ApplyFilter(criterias)
            .ApplySort(sortColumn, request.SortOrder)
            .Select(TenantConfigurationDto.Projection)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} configurations", configurations.TotalCount);

        return ApiResult<PaginatedList<TenantConfigurationDto>>.Success(configurations);
    }

    private static string NormalizeSortColumn(string? sortColumn)
    {
        if (string.IsNullOrWhiteSpace(sortColumn))
        {
            return nameof(IDP.Domain.AggregateRoots.Configuration.ConfigKey);
        }

        if (string.Equals(sortColumn, "Key", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(IDP.Domain.AggregateRoots.Configuration.ConfigKey);
        }

        if (string.Equals(sortColumn, "Value", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(IDP.Domain.AggregateRoots.Configuration.ConfigValue);
        }

        return sortColumn;
    }
}