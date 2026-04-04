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

    public async Task<(GrantTypes, int)> ValidateGrantType(string grantType, string clientId)
    {
        _logger.LogInfo("Validate Grant type {GrantType} for client:{ClientId}", grantType, clientId);

        var client = await _clientStore.GetClientShortInfo(clientId);

        if (client.GrantTypes == null || client.GrantTypes.Count == 0)
        {
            _logger.LogWarning("Client grant types not found.");

            throw new NotFoundException("Client grant types not found.");
        }

        if (!Enum.TryParse<GrantTypes>(grantType, ignoreCase: true, out var parsedGrantType)
            || !Enum.IsDefined(typeof(GrantTypes), parsedGrantType))
        {
            _logger.LogWarning("Grant type {GrantType} is unknown for Client: {ClientId}", grantType, clientId);

            throw new TokenRequestValidationException("unsupported_grant_type",
                "The requested grant_type is not supported.");
        }

        if (!SupportedTokenGrantTypes.IsSupported(parsedGrantType))
        {
            _logger.LogWarning("Grant type {GrantType} is not supported by the server for Client: {ClientId}",
                grantType, clientId);

            throw new TokenRequestValidationException("unsupported_grant_type",
                "The requested grant_type is not supported.");
        }

        if (client.GrantTypes.Contains(parsedGrantType))
        {
            return (parsedGrantType, client.TenantId);
        }

        _logger.LogWarning("Grant type {GrantType} is not allowed for Client: {ClientId}", grantType, clientId);

        throw new TokenRequestValidationException("unauthorized_client",
            "The client is not allowed to use the requested grant_type.");
    }
}
