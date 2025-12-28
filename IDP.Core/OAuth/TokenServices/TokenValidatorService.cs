using IDP.Core.Admin.Clients;
using IDP.Core.Admin.Roles;

namespace IDP.Core.OAuth.TokenServices;

internal class TokenValidatorService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleService _roleService;
    private readonly IAppLogger<TokenValidatorService> _logger;
    private readonly ClientService _clientService;
    private readonly AuthorizationCodeService _authorizationService;

    public TokenValidatorService(UserManager<User> userManager,
        RoleService roleService,
        ClientService clientService,
        IAppLogger<TokenValidatorService> logger,
        AuthorizationCodeService authorizationService)
    {
        _userManager = userManager;
        _roleService = roleService;
        _clientService = clientService;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    public async Task<TokenInfo> ValidateAuthorizationCodeAsync(TokenRequest tokenRequest)
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

        var validationResult = await _clientService.IsValidClient(tokenRequest.ClientId);

        if (validationResult != null && !validationResult.IsValidClient)
        {
            _logger.LogWarning("ClientId: {ClientId} is invalid", tokenRequest.ClientId);

            throw new NotFoundException("Client not found.");
        }

        return await ValidateTokenInfoAsync(tokenRequest.ClientId, authorizationCode.UserId);
    }

    public async Task<TokenInfo> ValidateTokenInfoAsync(string clientId, int userId)
    {
        _logger.LogInfo("Generating user info for token for user:{userId}", userId);

        var user = await _userManager.FindByIdAsync(userId.ToString());

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
            client.AccessTokenType,
            client.Scopes,
            client.Audiences,
            client.ClientSecretExpiry ?? 0,
            client.AccessTokenLifetime,
            client.RefreshTokenExpiration,
            distinctRoles);

        return userInfo;
    }

    public async Task<bool> ValidateGrantType(string grantType, string clientId)
    {
        _logger.LogInfo("Validate Grant type {GrantType} for client:{ClientId}", grantType, clientId);

        var client = await _clientService.GetClient(clientId);

        if (client == null)
        {
            _logger.LogWarning("Client not found.");

            throw new NotFoundException("Client not found.");
        }

        if (!Enum.IsDefined(typeof(GrantType), grantType))
        {
            _logger.LogWarning("Grant type not found for Client: {ClientId}", clientId);

            throw new NotFoundException("Grant type not found.");
        }

        if (client.GrantTypes.Contains(grantType))
        {
            return true;
        }

        return false;
    }
}
