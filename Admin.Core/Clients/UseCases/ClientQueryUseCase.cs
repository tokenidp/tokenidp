using Admin.Core.Common;

namespace Admin.Core.Clients.UseCases;

internal sealed class ClientQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ClientQueryUseCase> _logger;

    public ClientQueryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<ClientQueryUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<ClientDetail>> GetClientById(
        int clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching client {ClientId}", clientId);

        var client = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.Id == clientId
                && c.TenantId == _currentUserService.TenantId)
            .Select(ClientDetail.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found: {ClientId}", clientId);
            return ApiResult<ClientDetail>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        var provisioning = await _dbContext.ClientExternalProviders
            .AsNoTracking()
            .Where(c => c.ClientId == clientId && c.EnabledForClient)
            .Select(s => new
            {
                s.AutoCreateUsers,
                s.DefaultRoleId
            }).FirstOrDefaultAsync(cancellationToken);

        client.AutoCreateUsers = provisioning?.AutoCreateUsers ?? true;
        client.DefaultRoleId = provisioning?.DefaultRoleId;

        return ApiResult<ClientDetail>.Success(client);
    }

    public async Task<ApiResult<PaginatedList<ClientDetail>>> GetClients(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching clients list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.TenantId == _currentUserService.TenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(client =>
                (client.ClientName ?? string.Empty).ToLower().Contains(term) ||
                (client.ClientId ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase));
        if (statusCriteria != null)
        {
            criterias = criterias
                .Where(c => !string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (bool.TryParse(statusCriteria.Value, out var isActive))
            {
                query = query.Where(client => client.IsActive == isActive);
            }
        }

        var clients = await query
            .Select(ClientDetail.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} clients", clients.TotalCount);

        return ApiResult<PaginatedList<ClientDetail>>.Success(clients);
    }
}