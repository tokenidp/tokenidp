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

    internal async Task<TokenContext> BuildTokenContextAsync(
        string clientId,
        int userId,
        GrantTypes grantType,
        string? requestedScope,
        bool rememberMe = false)
    {
        _logger.LogInfo("Generating user info for token for user:{userId}", userId);

        var user = await _identityStore.GetUserShortInfo(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found with Id: {UserId}", userId);
            throw new NotFoundException("User not found.");
        }

        var userRoles = await _roleService.GetUserRoles(userId);

        if (!userRoles.IsSafe())
        {
            _logger.LogWarning("No active roles found for user {UserId}", userId);
            throw new NotFoundException("Roles not found.");
        }

        var distinctRoles = userRoles.Distinct().ToArray();
        var client = await _clientStore.GetActiveByClientId(clientId);
        var scopeSelection = ResolveScopeSelection(client, requestedScope);

        var tokenContext = TokenContext.Create(
            userId,
            user.TenantId,
            client.ClientName,
            user.UserName ?? string.Empty,
            clientId,
            grantType,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            rememberMe,
            distinctRoles,
            scopeSelection.Scopes,
            scopeSelection.Audiences);

        return tokenContext;
    }

    internal async Task<TokenContext> BuildClientCredentialTokenContextAsync(
        string clientId,
        string? requestedScope)
    {
        _logger.LogInfo("Generating token context for token for client:{clientId}", clientId);

        var client = await _clientStore.GetActiveByClientId(clientId);
        var scopeSelection = ResolveScopeSelection(client, requestedScope);

        return TokenContext.Create(
            client.TenantId,
            client.ClientName,
            clientId,
            GrantTypes.client_credentials,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            scopeSelection.Scopes,
            scopeSelection.Audiences,
            client.ActiveSecretHashes);
    }

    internal (string[] Scopes, string[] Audiences) ResolveScopeSelection(
        ClientValidationSnapshot client,
        string? requestedScope)
    {
        var requestedScopes = string.IsNullOrWhiteSpace(requestedScope)
            ? client.Scopes.ToArray()
            : requestedScope
                .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        var invalidScopes = requestedScopes
            .Where(scope => !client.Scopes.Contains(scope))
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            throw new TokenRequestValidationException(
                "invalid_scope",
                $"Invalid scope: {invalidScopes[0]} not found or not allowed");
        }

        var audiences = new HashSet<string>(StringComparer.Ordinal);

        foreach (var scopeName in requestedScopes)
        {
            if (StandardScopes.Supported.Contains(scopeName))
            {
                continue;
            }

            if (!client.TryGetApiResourceForScope(scopeName, out var apiResourceName))
            {
                throw new TokenRequestValidationException(
                    "invalid_scope",
                    $"Invalid scope: {scopeName} not found or not allowed");
            }

            if (!client.ApiResources.Contains(apiResourceName))
            {
                throw new TokenRequestValidationException(
                    "invalid_scope",
                    $"Scope {scopeName} belongs to ApiResource {apiResourceName} which is not assigned to this client");
            }

            audiences.Add(apiResourceName);
        }

        if (audiences.Count > 1)
        {
            throw new TokenRequestValidationException(
                "multiple_audiences_not_supported",
                $"Multiple audiences detected: {string.Join(", ", audiences.OrderBy(x => x))}. This IDP requires single audience per token request");
        }

        return (requestedScopes, audiences.ToArray());
    }
}