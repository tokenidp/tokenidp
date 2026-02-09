using IDP.Domain.AggregateRoots.Clients;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal sealed class GrantTypeValidatorUseCase
{
    private readonly IAppLogger<GrantTypeValidatorUseCase> _logger;
    private readonly IClientStore _clientStore;

    public GrantTypeValidatorUseCase(IAppLogger<GrantTypeValidatorUseCase> logger,
        IClientStore clientStore)
    {
        _logger = logger;
        _clientStore = clientStore;
    }

    public async Task<bool> ValidateGrantType(string grantType, string clientId)
    {
        _logger.LogInfo("Validate Grant type {GrantType} for client:{ClientId}", grantType, clientId);

        var client = await _clientStore.GetClientShortInfo(clientId);

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