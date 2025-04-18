using Microsoft.Extensions.Options;

namespace IDP.Service.Application.TokenService;

public class ReferenceTokenService : ITokenService, IReferenceTokenValidator
{
    private readonly IAppLogger<ReferenceTokenService> _logger;
    private readonly TokenSetting _jwtSettings;
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly RoleRepo _roleRepo;

    public ReferenceTokenService(UserManager<User> userManager,
        IOptions<TokenSetting> jwtSettings,
        ApplicationDbContext dbContext,
        TokenValidatorService tokenValidatorService,
        IAppLogger<ReferenceTokenService> logger,
        RoleRepo roleRepo)
    {
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
        _dbContext = dbContext;
        _tokenValidatorService = tokenValidatorService;
        _roleRepo = roleRepo;
    }

    public async Task<TokenResponse> GenerateToken(TokenRequest tokenRequest, string ipAddress)
    {
        _logger.LogInfo("Starting PKCE token generation for request from {IPAddress}", ipAddress);

        var response = await _tokenValidatorService.ValidatePkceAndAuthorizeAsync(tokenRequest, ipAddress);
        _logger.LogDebug("PKCE validation successful for user {UserId}", response.UserId);

        var tokenResponse = await CreateToken(response);
        _logger.LogInfo("Generated PKCE token {TokenId} for user {UserId}",
            $"{tokenResponse.AccessToken.SubstringSafe(0, 5)}...", response.UserId);

        return tokenResponse;
    }

    public async Task<TokenResponse> GenerateToken(int userId, int tenantId, string userName, string clientId)
    {
        _logger.LogInfo("Starting direct token generation for user {UserId} (Tenant: {TenantId}, " +
            "Client: {ClientId})", userId, tenantId, clientId);

        var userRoles = await _roleRepo.GetUserRoles(userId);

        if (!userRoles.IsSafe())
        {
            _logger.LogWarning("No active roles found for user {UserId}", userId);
            throw new NotFoundException("Roles not found.");
        }

        _logger.LogDebug("Found {RoleCount} roles for user {UserId}", userRoles.Distinct().Count(), userId);

        var userInfo = UserTokenInfo.Create(
            userId,
            tenantId,
            string.Empty,
            clientId,
            userRoles.Distinct().ToArray());

        var tokenResponse = await CreateToken(userInfo);

        _logger.LogInfo("Generated direct token {TokenId} for user {UserId}",
            tokenResponse.AccessToken.SubstringSafe(0, 5), userId);

        return tokenResponse;
    }

    public async Task<IntrospectionResponse> ValidateReferenceToken(string referenceToken)
    {
        _logger.LogDebug("Validating reference token: {TokenId}", referenceToken);

        var accessToken = await _dbContext.UserAccessToken
            .FirstOrDefaultAsync(s => s.TokenId == referenceToken && s.IsRevoked != true);

        if (accessToken == null)
        {
            _logger.LogWarning("Reference token not found or revoked: {TokenId}",
                $"{referenceToken.SubstringSafe(0, 5)}...");

            return IntrospectionResponse.Create();
        }

        _logger.LogDebug("Valid token found for user {UserId}", accessToken.UserId);

        return IntrospectionResponse.Create(
            accessToken.UserId,
            accessToken.TenantId,
            accessToken.Scopes,
            accessToken.Roles.Split(","));
    }

    private async Task<TokenResponse> CreateToken(UserTokenInfo userInfo)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
        var token = Guid.NewGuid().ToString();

        _logger.LogDebug("Creating access token for user {UserId} with expiry {Expiry}",
            userInfo.UserId, expiry);

        var accessToken = new UserAccessToken(
            userInfo.UserId,
            userInfo.TenantId,
            userInfo.ClientId,
            token,
            "Profile",
            expiry,
            DateTime.UtcNow,
            string.Join(",", userInfo.Roles),
            userInfo.UserId);

        _dbContext.UserAccessToken.Add(accessToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Access token saved to database with ID: {TokenId}",
            $"{token.SubstringSafe(0, 5)}...");

        return TokenResponse.Create(userInfo.UserId, token, expiry);
    }
}