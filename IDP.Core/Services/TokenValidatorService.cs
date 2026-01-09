namespace IDP.Core.Services;

internal sealed class TokenValidatorService
{
    private readonly IIdentityStore _identityStore;
    private readonly IRoleStore _roleService;
    private readonly IAppLogger<TokenValidatorService> _logger;
    private readonly ClientService _clientService;
    private readonly AuthorizationCodeService _authorizationService;

    public TokenValidatorService(IRoleStore roleService,
        ClientService clientService,
        AuthorizationCodeService authorizationService,
        IAppLogger<TokenValidatorService> logger,
        IIdentityStore identityStore)
    {
        _roleService = roleService;
        _clientService = clientService;
        _logger = logger;
        _authorizationService = authorizationService;
        _identityStore = identityStore;
    }

    internal async Task<TokenInfo> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest)
    {
        _logger.LogInfo("Token request received for ClientId: {ClientId} with Code: {Code}",
            tokenRequest.ClientId, tokenRequest.Code);

        var authorizationCode = await _authorizationService
            .ValidateAuthorizationCode(tokenRequest.Code);

        var calculatedCodeChallenge = PkceHelper.CalculateCodeChallenge(tokenRequest.CodeVerifier);

        if (calculatedCodeChallenge != authorizationCode.CodeChallenge)
        {
            _logger.LogWarning("Invalid code verifier for ClientId: {ClientId}, UserId: {UserId}",
                tokenRequest.ClientId, authorizationCode.UserId);

            throw new UnauthorizedAccessException("Invalid code verifier.");
        }

        var validationResult = await _clientService.ValidateClient(tokenRequest.ClientId);

        if (validationResult != null && !validationResult.IsValidClient)
        {
            _logger.LogWarning("ClientId: {ClientId} is invalid", tokenRequest.ClientId);

            throw new NotFoundException("Client not found.");
        }

        var tokenInfo = await ValidateTokenInfoAsync(tokenRequest.ClientId, authorizationCode.UserId);

        tokenInfo.AddAuthorizedScopes(authorizationCode.Scopes);

        return tokenInfo;
    }

    internal async Task<TokenInfo> ValidateTokenInfoAsync(string clientId, int userId)
    {
        _logger.LogInfo("Generating user info for token for user:{userId}", userId);

        var user = await _identityStore.FindByIdAsync(userId.ToString());

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

        var client = await _clientService.GetClient(clientId);

        if (client == null)
        {
            _logger.LogWarning("Client not found.");

            throw new NotFoundException("Client not found.");
        }

        _logger.LogInfo("Roles found for UserId: {UserId} => {Roles}", userId, string.Join(", ", userRoles));

        var userInfo = TokenInfo.Create(userId,
            user.TenantId,
            user.UserName,
            clientId,
            client.TokenType,
            client.Scopes.ToArray(),
            client.Audiences.ToArray(),
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            distinctRoles);

        return userInfo;
    }

    internal async Task<bool> ValidateGrantType(string grantType, string clientId)
    {
        _logger.LogInfo("Validate Grant type {GrantType} for client:{ClientId}", grantType, clientId);

        var client = await _clientService.GetClient(clientId);

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
