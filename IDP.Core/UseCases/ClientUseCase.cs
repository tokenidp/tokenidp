using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class ClientUseCase : IClientUseCase
{
    private readonly IAppLogger<ClientUseCase> _logger;
    private readonly IClientStore _clientStore;

    public ClientUseCase(IAppLogger<ClientUseCase> logger,
        IClientStore clientStore)
    {
        _logger = logger;
        _clientStore = clientStore;
    }

    public async Task<ClientValidationSnapshot> GetClient(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId);

        var client = await _clientStore.GetByClientId(clientId);

        _logger.LogDebug("Retrieved client {ClientId}", clientId);

        return client ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientValidationResult> ValidateClient(string clientId)
    {
        _logger.LogDebug("IsValidClient: Checking is valid client for client: {ClientId}", clientId);

        var client = await GetClient(clientId);

        return ClientValidationResult.Create(client != null,
            client?.RedirectUri ?? string.Empty,
            client?.Scopes);
    }

    public async Task<bool> ValidateGrantType(string grantType, string clientId)
    {
        _logger.LogInfo("Validate Grant type {GrantType} for client:{ClientId}", grantType, clientId);

        var client = await GetClient(clientId);

        if (client == null)
        {
            _logger.LogWarning("Client not found.");

            throw new NotFoundException("Client not found.");
        }

        if (client.GrantTypes == null || client.GrantTypes.Count == 0)
        {
            _logger.LogWarning("Client grant types not found.");

            throw new NotFoundException("Client grant types not found.");
        }

        if (!Enum.IsDefined(typeof(GrantTypes), grantType))
        {
            _logger.LogWarning("Grant type not found for Client: {ClientId}", clientId);

            throw new NotFoundException("Grant type not found.");
        }

        if (client.GrantTypes.Any(gt => gt.ToString() == grantType))
        {
            return true;
        }

        return false;
    }
}