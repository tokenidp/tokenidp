using TokenIDP.Core.Abstractions.Repositories;

namespace TokenIDP.Core.Admin.Tokens.UseCases;

internal sealed class TokenQueryUseCase
{
    private readonly ITokenRepository _tokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TokenQueryUseCase> _logger;

    public TokenQueryUseCase(
        ITokenRepository tokenRepository,
        ICurrentUserService currentUserService,
        IAppLogger<TokenQueryUseCase> logger)
    {
        _tokenRepository = tokenRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<PaginatedList<TokenListItem>>> GetTokens(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tokens list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var tokens = await _tokenRepository.SearchTokensAsync(
            _currentUserService.TenantId,
            request,
            cancellationToken);

        _logger.LogDebug("Fetched {Count} tokens", tokens.TotalCount);

        return ApiResult<PaginatedList<TokenListItem>>.Success(tokens);
    }

    public async Task<ApiResult<TokenDetail>> GetTokenById(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching token {TokenId}", tokenId);

        var token = await _tokenRepository.GetTokenDetailAsync(
            _currentUserService.TenantId,
            tokenId,
            cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found: {TokenId}", tokenId);
            return ApiResult<TokenDetail>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        return ApiResult<TokenDetail>.Success(token);
    }
}

