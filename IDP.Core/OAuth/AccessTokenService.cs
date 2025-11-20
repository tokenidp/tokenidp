using IDP.Core.Admin.Roles;

namespace IDP.Core.TokenServices;

internal class AccessTokenService : ITokenService
{
    private readonly IAppLogger<AccessTokenService> _logger;
    private readonly TokenSetting _jwtSettings;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly RoleService _roleService;

    public AccessTokenService(UserManager<User> userManager,
        IOptions<TokenSetting> jwtSettings,
        JwtTokenGenerator tokenGenerator,
        RoleService roleService,
        IAppLogger<AccessTokenService> appLogger,
        TokenValidatorService tokenValidatorService)
    {
        _jwtSettings = jwtSettings.Value;
        _tokenGenerator = tokenGenerator;
        _roleService = roleService;
        _logger = appLogger;
        _tokenValidatorService = tokenValidatorService;
    }

    public async Task<TokenResponse> GenerateToken(TokenRequest tokenRequest, string ipAddress)
    {
        _logger.LogInfo("Generating token for request from {IPAddress}", ipAddress);

        var userInfo = await _tokenValidatorService.ValidatePkceAndAuthorizeAsync(tokenRequest, ipAddress);
        _logger.LogDebug("User validation successful for {UserId}", userInfo.UserId);

        var token = CreateToken(userInfo);
        _logger.LogInfo("Token generated successfully for {UserId}", userInfo.UserId);

        return token;
    }

    public async Task<TokenResponse> GenerateToken(int userId, int tenantId, string userName, string clientId)
    {
        _logger.LogInfo("Generating direct token for {UserName} (UserId: {UserId}, Tenant: {TenantId}, " +
            "Client: {ClientId})", userName, userId, tenantId, clientId);

        var userRoles = await _roleService.GetUserRoles(userId);

        if (!userRoles.IsSafe())
        {
            _logger.LogWarning("No active roles found for user {UserId}", userId);
            throw new NotFoundException("Roles not found.");
        }

        var distinctRoles = userRoles.Distinct().ToArray();
        _logger.LogDebug("Found {RoleCount} roles for user {UserId}", distinctRoles.Length, userId);

        var userInfo = UserTokenInfo.Create(userId, tenantId, userName, clientId, distinctRoles);
        var token = CreateToken(userInfo);

        _logger.LogInfo("Direct token generated successfully for {UserId}", userId);
        return token;
    }

    private TokenResponse CreateToken(UserTokenInfo userTokenInfo)
    {
        var tokenId = Guid.NewGuid().ToString();
        _logger.LogDebug("Creating token (ID: {TokenId}) for {UserId} with roles: {Roles}",
            tokenId, userTokenInfo.UserId, string.Join(",", userTokenInfo.Roles));

        var accessToken = _tokenGenerator.GetAccessToken(
            tokenId,
            userTokenInfo.UserId.ToString(),
            userTokenInfo.UserName,
            userTokenInfo.TenantId.ToString(),
            userTokenInfo.ClientId,
            "Profile",
            userTokenInfo.Roles);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
        _logger.LogDebug("Token will expire at {ExpirationTime}", expiresAt);

        return TokenResponse.Create(userTokenInfo.UserId, accessToken, expiresAt);
    }
}
