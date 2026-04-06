using TokenIDP.Domain.AggregateRoots.Tokens;
using TokenIDP.Core.Foundation.Abstractions.Stores;

namespace TokenIDP.Core.Admin.Tokens.UseCases;

internal sealed class TokenCommandUseCase
{
    private readonly ITokenStore _tokenStore;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IAppLogger<TokenCommandUseCase> _logger;

    public TokenCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TokenCommandUseCase> logger,
        ITokenStore tokenStore)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
        _tokenStore = tokenStore;
    }

    public async Task<ApiResult<int>> RevokeToken(
        Guid tokenId,
        string ipAddress,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.Tokens
            .Include(t => t.ReferenceToken)
            .Include(t => t.RefreshToken)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.TokenStatus != TokenStatus.Revoked
            && t.TenantId == _currentUserService.TenantId, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found for revoke: {TokenId}", tokenId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "Admin revocation"
            : reason.Trim();

        token.Revoke(normalizedReason, ipAddress, _currentUserService.UserId);

        var result = await _tokenStore.RevokeToken(token);

        _logger.LogInfo("Token revoked {TokenId}", tokenId);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> ExpireToken(
        Guid tokenId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.Tokens
            .Include(t => t.ReferenceToken)
            .Include(t => t.RefreshToken)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.TokenStatus != TokenStatus.Revoked
            && t.TenantId == _currentUserService.TenantId, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found for expire: {TokenId}", tokenId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        token.Expire(_currentUserService.UserId);

        _dbContext.Tokens.Update(token);

        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInfo("Token expired {TokenId}", tokenId);

        return ApiResult<int>.Success(result);
    }
}
