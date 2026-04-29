using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Clients.UseCases;

internal sealed class ClientQueryUseCase
{
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ClientQueryUseCase> _logger;

    public ClientQueryUseCase(
        IClientRepository clientRepository,
        ICurrentUserService currentUserService,
        IAppLogger<ClientQueryUseCase> logger)
    {
        _clientRepository = clientRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<ClientDetail>> GetClientById(
        int clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching client {ClientId}", clientId);

        var client = await _clientRepository.GetClientDetailAsync(
            _currentUserService.TenantId,
            clientId,
            cancellationToken);

        if (client == null)
        {
            _logger.LogWarning("Client not found: {ClientId}", clientId);
            return ApiResult<ClientDetail>.Failure(ApiError.Failure("NotFound",
                "Client not found for the Id {0}".FormatString(clientId)));
        }

        return ApiResult<ClientDetail>.Success(client);
    }

    public async Task<ApiResult<PaginatedList<ClientDetail>>> GetClients(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching clients list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var clients = await _clientRepository.SearchClientsAsync(
            _currentUserService.TenantId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} clients", clients.TotalCount);

        return ApiResult<PaginatedList<ClientDetail>>.Success(clients);
    }
}
