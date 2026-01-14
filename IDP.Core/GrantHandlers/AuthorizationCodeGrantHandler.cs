namespace IDP.Core.GrantHandlers;

internal sealed class AuthorizationCodeGrantHandler : ITokenGrantHandler
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
        if (tokenRequest is null)
        {
            throw new ArgumentNullException(nameof(tokenRequest));
        }

        _logger.LogInfo("Generating token for request from {IPAddress}", tokenRequest.IpAddress);

        var tokenInfo = await _tokenValidatorService.ValidateAuthorizationCodeAsync(tokenRequest);

        _logger.LogDebug("User validation successful for {UserId}", tokenInfo.UserId);

        var scopes = ResolveScopes(tokenInfo.Scopes);

        var includeRefreshToken = scopes.Contains("offline_access");

        string refreshToken = string.Empty;

        if (includeRefreshToken)
        {
            refreshToken = await _tokenService.CreateRefreshToken(tokenInfo.UserId,
                tokenRequest.IpAddress,
                tokenInfo.RefreshTokenExpiration);
        }

        var token = await _tokenService.CreateToken(tokenInfo);

        token.AddRefreshToken(refreshToken);

        _logger.LogInfo("Token generated successfully for {UserId}", tokenInfo.UserId);

        return token;
    }

    private static HashSet<string> ResolveScopes(string[] clientScopes)
    {
        var scopes = clientScopes.Count() > 0 ? string.Join(' ', clientScopes) : string.Empty;

        if (string.IsNullOrWhiteSpace(scopes))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
