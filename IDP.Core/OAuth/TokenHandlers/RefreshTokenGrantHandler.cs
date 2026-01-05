using IDP.Core.Model;
using IDP.Core.OAuth.Interfaces;

namespace IDP.Core.OAuth.TokenHandlers;

internal sealed class RefreshTokenGrantHandler : ITokenGrantHandler
{
    private readonly IAppLogger<RefreshTokenGrantHandler> _logger;
    private readonly TokenValidatorService _tokenValidatorService;
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenService _tokenService;

    public RefreshTokenGrantHandler(JwtTokenGenerator tokenGenerator,
        IAppLogger<RefreshTokenGrantHandler> logger,
        TokenValidatorService tokenValidatorService,
        ApplicationDbContext dbContext,
        TokenService tokenService)
    {
        _logger = logger;
        _tokenValidatorService = tokenValidatorService;
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> HandleAsync(TokenRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var existingRefreshToken = await _dbContext.RefreshTokens.Where(t => t.RefreshToken == request.RefreshToken)
                    .FirstOrDefaultAsync();

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
