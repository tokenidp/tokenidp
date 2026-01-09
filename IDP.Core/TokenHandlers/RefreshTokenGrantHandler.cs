namespace IDP.Core.TokenHandlers;

internal sealed class RefreshTokenGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<RefreshTokenGrantHandler> _logger;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly ITokenStore _tokenStore;
    private readonly TokenService _tokenService;

    public RefreshTokenGrantHandler(JwtTokenGenerator tokenGenerator,
        IAppLogger<RefreshTokenGrantHandler> logger,
        TokenValidatorService tokenValidatorService,
        TokenService tokenService,
        ITokenStore tokenStore)
    {
        _logger = logger;
        _tokenValidatorService = tokenValidatorService;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var existingRefreshToken = await _tokenStore.GetRefreshToken(request.RefreshToken ?? string.Empty);

        if (existingRefreshToken == null)
        {
            _logger.LogWarning("Refresh token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        var tokenInfo = await _tokenValidatorService.ValidateTokenInfoAsync(request.ClientId, existingRefreshToken.UserId);

        _logger.LogInfo("Generating refresh token for client {ClientId} from {IPAddress}", request.ClientId, request.IpAddress);

        var refreshToken = await _tokenService.CreateRefreshToken(existingRefreshToken.UserId,
            request.IpAddress,
            tokenInfo.RefreshTokenExpiration);

        var token = await _tokenService.CreateToken(tokenInfo);

        token.AddRefreshToken(refreshToken);

        _logger.LogInfo("Successfully saved new refresh token for user {UserId}", existingRefreshToken.UserId);

        return token;
    }
}
