using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.UseCases;

internal class TokenContextUseCase
{
    private readonly IUserStore _identityStore;
    private readonly IRoleStore _roleService;
    private readonly IAppLogger<TokenContextUseCase> _logger;
    private readonly IClientStore _clientStore;

    public TokenContextUseCase(IRoleStore roleService,
        IClientStore clientStore,
        IAppLogger<TokenContextUseCase> logger,
        IUserStore identityStore)
    {
        _roleService = roleService;
        _clientStore = clientStore;
        _logger = logger;
        _identityStore = identityStore;
    }

    internal async Task<TokenContext> BuildTokenContextAsync(string clientId,
        int userId,
        bool rememberMe = false)
    {
        _logger.LogInfo("Generating user info for token for user:{userId}", userId);

        var user = await _identityStore.GetUserShortInfo(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found with Id: {UserId}", userId);

            throw new NotFoundException("User not found.");
        }

        _logger.LogInfo("User found: {UserName}", user.UserName ?? "user.Username is empty.");

        var userRoles = await _roleService.GetUserRoles(userId);

        if (!userRoles.IsSafe())
        {
            _logger.LogWarning("No active roles found for user {UserId}", userId);

            throw new NotFoundException("Roles not found.");
        }

        var distinctRoles = userRoles.Distinct().ToArray();

        var client = await _clientStore.GetActiveByClientId(clientId);

        if (client == null)
        {
            _logger.LogWarning("Client not found.");

            throw new NotFoundException("Client not found.");
        }

        _logger.LogInfo("Roles found for UserId: {UserId} => {Roles}", userId, string.Join(", ", userRoles));

        var userInfo = TokenContext.Create(userId,
            user.TenantId,
            client.ClientName,
            user.UserName ?? string.Empty,
            clientId,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            rememberMe,
            distinctRoles,
            client.Scopes.ToArray(),
            client.Audiences.ToArray());

        return userInfo;
    }

    internal async Task<TokenContext> BuildClientCredentialTokenContextAsync(string clientId)
    {
        _logger.LogInfo("Generating token context for token for client:{clientId}", clientId);

        var client = await _clientStore.GetActiveByClientId(clientId);

        if (client == null)
        {
            _logger.LogWarning("Client not found.");

            throw new NotFoundException("Client not found.");
        }

        var userInfo = TokenContext.Create(
            client.TenantId,
            client.ClientName,
            clientId,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            client.Scopes.ToArray(),
            client.Audiences.ToArray(),
            client.ActiveSecretHashes);

        return userInfo;
    }
}
