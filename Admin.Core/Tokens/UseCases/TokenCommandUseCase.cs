using IDP.Domain.AggregateRoots.Tokens;

namespace Admin.Core.Tokens.UseCases;

internal sealed class TokenCommandUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TokenCommandUseCase> _logger;

    public TokenCommandUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TokenCommandUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<int>> RevokeToken(
        int tokenId,
        string ipAddress,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.TokenSearch
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.TenantId == _currentUserService.TenantId,
                cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found for revoke: {TokenId}", tokenId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "Admin revocation"
            : reason.Trim();

        if (string.Equals(token.SourceType, "RefreshToken", StringComparison.OrdinalIgnoreCase))
        {
            //var refreshToken = await _dbContext.RefreshTokens
            //    .FirstOrDefaultAsync(t => t.Id == token.SourceTokenId, cancellationToken);

            RefreshToken refreshToken = default!;

            if (refreshToken == null)
            {
                return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                    "Refresh token not found for the Id {0}".FormatString(token.TokenId)));
            }

            //refreshToken.RevokeToken(ipAddress, normalizedReason);
            //refreshToken.SetUpdated(_currentUserService.UserId);
        }
        else if (string.Equals(token.SourceType, "ReferenceToken", StringComparison.OrdinalIgnoreCase))
        {
            //var referenceToken = await _dbContext.ReferenceTokens
            //    .FirstOrDefaultAsync(t => t.Id == token.SourceTokenId, cancellationToken);

            ReferenceToken referenceToken = default!;

            if (referenceToken == null)
            {
                return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                    "Reference token not found for the Id {0}".FormatString(token.TokenId)));
            }

            //referenceToken.RevokeToken(_currentUserService.UserId);
        }
        else
        {
            return ApiResult<int>.Failure(ApiError.Failure("token.unsupported",
                "Token type does not support revocation."));
        }

        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInfo("Token revoked {TokenId}", tokenId);

        return ApiResult<int>.Success(result);
    }

    public async Task<ApiResult<int>> ExpireToken(
        int tokenId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.TokenSearch
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.TenantId == _currentUserService.TenantId,
                cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found for expire: {TokenId}", tokenId);
            return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        if (string.Equals(token.SourceType, "RefreshToken", StringComparison.OrdinalIgnoreCase))
        {
            //var refreshToken = await _dbContext.RefreshTokens
            //    .FirstOrDefaultAsync(t => t.Id == token.SourceTokenId, cancellationToken);

            RefreshToken refreshToken = default!;

            if (refreshToken == null)
            {
                return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                    "Refresh token not found for the Id {0}".FormatString(token.TokenId)));
            }

            //refreshToken.ExpireNow();
            //refreshToken.SetUpdated(_currentUserService.UserId);
        }
        else if (string.Equals(token.SourceType, "ReferenceToken", StringComparison.OrdinalIgnoreCase))
        {
            //var referenceToken = await _dbContext.ReferenceTokens
            //    .FirstOrDefaultAsync(t => t.Id == token.SourceTokenId, cancellationToken);

            ReferenceToken referenceToken = default!;

            if (referenceToken == null)
            {
                return ApiResult<int>.Failure(ApiError.Failure("NotFound",
                    "Reference token not found for the Id {0}".FormatString(token.TokenId)));
            }

            //referenceToken.ExpireNow(_currentUserService.UserId);
        }
        else
        {
            return ApiResult<int>.Failure(ApiError.Failure("token.unsupported",
                "Token type does not support force expiration."));
        }

        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInfo("Token expired {TokenId}", tokenId);

        return ApiResult<int>.Success(result);
    }
}