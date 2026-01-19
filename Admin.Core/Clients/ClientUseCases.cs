using Admin.Core.Common;

namespace Admin.Core.Clients;

internal class ClientUseCases
{
    private readonly IAppLogger<ClientUseCases> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public ClientUseCases(IApplicationDbContext dbContext,
        IAppLogger<ClientUseCases> logger,
        ICache cache,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResult<int>> CreateClient(CreateUpdateClient request)
    {
        _logger.LogDebug("Creating client for tenant {TenantId}", request.TenantId);

        var client = CreateNewClient(request);

        _dbContext.Clients.Add(client);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Client created with Id {ClientId}", client.Id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> UpdateClient(int id, CreateUpdateClient request)
    {
        _logger.LogDebug("Updating client {ClientId}", id);

        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            _logger.LogWarning("Client not found for update: {ClientId}", id);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(id)));
        }

        MapClientUpdate(client, request);

        _dbContext.Clients.Update(client);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Client updated {ClientId}", id);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<ClientDto>> GetClientById(int clientId)
    {
        _logger.LogDebug("Fetching client {ClientId}", clientId);

        var client = await _dbContext.Clients
            .Where(c => c.Id == clientId)
            .Select(ClientDto.Projection)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            _logger.LogWarning("Client not found: {ClientId}", clientId);
            return ApiResult<ClientDto>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        return ApiResult<ClientDto>.Success(client);
    }

    public async Task<ApiResult<PaginatedList<ClientDto>>> GetClients(SearchData request)
    {
        _logger.LogDebug("Fetching clients list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Select(ClientDto.Projection)
            .ApplyFilter(request.SearchCriterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} clients", clients.TotalCount);

        return ApiResult<PaginatedList<ClientDto>>.Success(clients);
    }

    private Client CreateNewClient(CreateUpdateClient request)
    {
        var tenantId = request.TenantId == 0
            ? _currentUserService.TenantId
            : request.TenantId;

        return new Client(
            tenantId,
            request.ClientId,
            request.ClientName,
            request.Description,
            request.ClientType,
            request.AppType,
            request.AccessTokenType,
            request.RedirectUri,
            request.LogoutRedirectUri,
            request.IsActive,
            request.ClientSecretExpiry,
            request.AccessTokenLifetime,
            request.AuthorizationCodeLifetime,
            request.RefreshTokenExpiration,
            request.PermitLimit,
            request.TimeWindow,
            request.QueueLimit,
            request.EnableITracking);
    }

    private void MapClientUpdate(Client client, CreateUpdateClient request)
    {
        client.UpdateClient(
            request.ClientId,
            request.ClientName,
            request.Description,
            request.ClientType,
            request.AppType,
            request.AccessTokenType,
            request.RedirectUri,
            request.LogoutRedirectUri,
            request.IsActive,
            request.ClientSecretExpiry,
            request.AccessTokenLifetime,
            request.AuthorizationCodeLifetime,
            request.RefreshTokenExpiration,
            request.PermitLimit,
            request.TimeWindow,
            request.QueueLimit,
            request.EnableITracking);
    }
}
