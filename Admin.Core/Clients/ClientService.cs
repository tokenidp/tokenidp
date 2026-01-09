namespace Admin.Core.Clients;

internal class ClientService
{
    private readonly IAppLogger<ClientService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICache _cache;

    public ClientService(IApplicationDbContext dbContext,
        IAppLogger<ClientService> logger,
        ICache cache,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<Result> CreateClient(CreateUpdateClient request)
    {
        _logger.LogDebug("Creating client for tenant {TenantId}", request.TenantId);

        var client = CreateNewClient(request);

        _dbContext.Clients.Add(client);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Client created with Id {ClientId}", client.Id);

        return Result.Success(result);
    }

    public async Task<Result> UpdateClient(int id, CreateUpdateClient request)
    {
        _logger.LogDebug("Updating client {ClientId}", id);

        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            _logger.LogWarning("Client not found for update: {ClientId}", id);
            return Result.Failure("NotFound", "Client not found for the Id {0}".FormatString(id));
        }

        MapClientUpdate(client, request);

        _dbContext.Clients.Update(client);

        var result = await _dbContext.SaveChangesAsync();

        _logger.LogInfo("Client updated {ClientId}", id);

        return Result.Success(result);
    }

    public async Task<ClientDto?> GetClientById(int clientId)
    {
        _logger.LogDebug("Fetching client {ClientId}", clientId);

        var client = await _dbContext.Clients
            .Where(c => c.Id == clientId)
            .Select(ClientDto.Projection)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            _logger.LogWarning("Client not found: {ClientId}", clientId);
        }

        return client;
    }

    public async Task<PaginatedList<ClientDto>> GetClients(SearchData request)
    {
        _logger.LogDebug("Fetching clients list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Select(ClientDto.Projection)
            .ApplyFilter(request.SearchCriterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .PaginatedTo(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} clients", clients.TotalCount);

        return clients;
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
