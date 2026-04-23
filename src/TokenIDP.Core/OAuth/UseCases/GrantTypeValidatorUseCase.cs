using TokenIDP.Domain.AggregateRoots.Clients;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.OAuth.Model;

namespace TokenIDP.Core.OAuth.UseCases;

internal sealed class GrantTypeValidatorUseCase
{
    private readonly IAppLogger<GrantTypeValidatorUseCase> _logger;
    private readonly IClientRepository _clientStore;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public GrantTypeValidatorUseCase(IAppLogger<GrantTypeValidatorUseCase> logger,
        IClientRepository clientStore,
        ITenantContextAccessor tenantContextAccessor)
    {
        _logger = logger;
        _clientStore = clientStore;
        _tenantContextAccessor = tenantContextAccessor;
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

        if (!TokenGrantTypeNames.TryParse(grantType, out var parsedGrantType))
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
            return (parsedGrantType, ResolveRequestTenantId(client));
        }

        _logger.LogWarning("Grant type {GrantType} is not allowed for Client: {ClientId}", grantType, clientId);

        throw new TokenRequestValidationException("unauthorized_client",
            "The client is not allowed to use the requested grant_type.");
    }

    private int ResolveRequestTenantId(ClientShortInfo client)
    {
        return _tenantContextAccessor.HasTenant
            ? _tenantContextAccessor.TenantId
            : client.TenantId;
    }
}


