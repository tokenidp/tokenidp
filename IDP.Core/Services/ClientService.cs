namespace IDP.Core.Services;

internal sealed class ClientService
{
    private readonly IAppLogger<ClientService> _logger;
    private readonly IClientStore _clientStore;

    public ClientService(IAppLogger<ClientService> logger,
        IClientStore clientStore)
    {
        _logger = logger;
        _clientStore = clientStore;
    }

    internal async Task<ClientValidationSnapshot> GetClient(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId);

        var client = await _clientStore.GetByClientId(clientId);

        _logger.LogDebug("Retrieved client {ClientId}", clientId);

        return client ?? throw new NotFoundException("Client not found.");
    }

    internal async Task<ClientValidationResult> ValidateClient(string clientId)
    {
        _logger.LogDebug("IsValidClient: Checking is valid client for client: {ClientId}", clientId);

        var client = await GetClient(clientId);

        return ClientValidationResult.Create(client != null,
            client?.RedirectUri ?? string.Empty,
            client?.Scopes);
    }
}