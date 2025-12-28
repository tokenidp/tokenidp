namespace IDP.Core.OAuth.TokenServices;

internal class AuthorizationCodeGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<AuthorizationCodeGrantHandler> _logger;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly TokenService _tokenService;

    public AuthorizationCodeGrantHandler(IAppLogger<AuthorizationCodeGrantHandler> logger,
        TokenValidatorService tokenValidatorService,
        TokenService tokenService)
    {
        _logger = logger;
        _tokenValidatorService = tokenValidatorService;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest tokenRequest)
    {
        _logger.LogInfo("Generating token for request from {IPAddress}", tokenRequest.IpAddress);

        var tokenInfo = await _tokenValidatorService.ValidateAuthorizationCodeAsync(tokenRequest);

        _logger.LogDebug("User validation successful for {UserId}", tokenInfo.UserId);

        var token = _tokenService.CreateAccessToken(tokenInfo);

        _logger.LogInfo("Token generated successfully for {UserId}", tokenInfo.UserId);

        return token;
    }
}
