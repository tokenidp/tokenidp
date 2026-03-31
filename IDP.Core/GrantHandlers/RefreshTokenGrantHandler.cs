using IDP.Core.UseCases;
using IDP.Foundation.Abstractions.Stores;

namespace IDP.Core.GrantHandlers;

internal sealed class RefreshTokenGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<RefreshTokenGrantHandler> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly TokenIssuerUseCase _tokenService;
    private readonly TokenContextUseCase _tokenContextUseCase;
    private readonly TokenSecretGenerator _tokenSecretGenerator;

    public RefreshTokenGrantHandler(JwtTokenGenerator tokenGenerator,
        IAppLogger<RefreshTokenGrantHandler> logger,
        ITokenStore tokenStore,
        TokenContextUseCase tokenContextUseCase,
        TokenIssuerUseCase tokenService,
        TokenSecretGenerator tokenSecretGenerator)
    {
        _logger = logger;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
        _tokenContextUseCase = tokenContextUseCase;
        _tokenSecretGenerator = tokenSecretGenerator;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new ArgumentNullException(nameof(request));
        }

        var tokenHash = _tokenSecretGenerator.HashToken(request.RefreshToken!);

        var existingToken = await _tokenStore.GetRefreshToken(tokenHash);

        if (existingToken == null)
        {
            _logger.LogWarning("Refresh token not found.");

            throw new NotFoundException("Refresh token not found.");
        }

        var tokenInfo = await _tokenContextUseCase
            .BuildTokenContextAsync(
                request.ClientId,
                existingToken.UserId ?? 0,
                GrantTypes.refresh_token,
                request.Scope.IsSafe() ? request.Scope : existingToken.Scope);

        _logger.LogInfo("Generating refresh token for client {ClientId} from {IPAddress}",
            request.ClientId, request.IpAddress ?? string.Empty);

        var token = await _tokenService.IssueTokenAsync(tokenInfo);

        _logger.LogInfo("Successfully saved new refresh token for client {clientId}", existingToken.ClientId);

        return token;
    }
}