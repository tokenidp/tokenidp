using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.OAuth.UseCases;

internal class TokenContextUseCase
{
    private readonly IUserRepository _identityStore;
    private readonly IRoleRepository _roleService;
    private readonly IAppLogger<TokenContextUseCase> _logger;
    private readonly IClientRepository _clientStore;
    private readonly ITenantRepository _tenantStore;

    public TokenContextUseCase(IRoleRepository roleService,
        IClientRepository clientStore,
        ITenantRepository tenantStore,
        IAppLogger<TokenContextUseCase> logger,
        IUserRepository identityStore)
    {
        _roleService = roleService;
        _clientStore = clientStore;
        _tenantStore = tenantStore;
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
        var activeTenant = await GetTenantSummaryAsync(user.TenantId);
        var authTenant = await GetTenantSummaryAsync(client.TenantId);

        var tokenContext = TokenContext.Create(
            userId,
            activeTenant.Id,
            activeTenant.TenantKey,
            authTenant.Id,
            authTenant.TenantKey,
            client.ClientName,
            user.UserName ?? string.Empty,
            clientId,
            grantType,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            client.RefreshTokenDeliveryMode,
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
        var tenant = await GetTenantSummaryAsync(client.TenantId);

        return TokenContext.Create(
            tenant.Id,
            tenant.TenantKey,
            client.ClientName,
            clientId,
            GrantTypes.client_credentials,
            client.TokenType,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            client.RefreshTokenDeliveryMode,
            scopeSelection.Scopes,
            scopeSelection.Audiences,
            client.ActiveSecretHashes);
    }

    private async Task<TenantSummary> GetTenantSummaryAsync(int tenantId)
    {
        var tenant = await _tenantStore.GetSummaryAsync(tenantId, CancellationToken.None);

        if (tenant is null)
        {
            throw new NotFoundException($"Tenant {tenantId} not found.");
        }

        return tenant;
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

