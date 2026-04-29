using TokenIDP.Core.OAuth.UseCases;

namespace TokenIDP.Core.OAuth.GrantHandlers;

internal sealed class AuthorizationCodeGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<AuthorizationCodeGrantHandler> _logger;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly IAuthorizationCodeUseCase _authorizationCodeUseCase;

    public AuthorizationCodeGrantHandler(IAppLogger<AuthorizationCodeGrantHandler> logger,
        TokenIssuerUseCase tokenService,
        IAuthorizationCodeUseCase authorizationCodeUseCase)
    {
        _logger = logger;
        _tokenService = tokenService;
        _authorizationCodeUseCase = authorizationCodeUseCase;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest tokenRequest)
    {
        if (tokenRequest is null)
        {
            throw new ArgumentNullException(nameof(tokenRequest));
        }

        _logger.LogInfo("Generating token for request from {IPAddress}", tokenRequest.IpAddress ?? string.Empty);

        var tokenInfo = await _authorizationCodeUseCase.ValidateAuthorizationCodeAsync(tokenRequest);

        _logger.LogDebug("User validation successful for {UserId}", tokenInfo.UserId ?? 0);

        var token = await _tokenService.IssueTokenAsync(tokenInfo);

        _logger.LogInfo("Token generated successfully for {UserId}", tokenInfo.UserId ?? 0);

        return token;
    }
}

