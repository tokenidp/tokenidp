using Admin.Core.Common;

namespace Admin.Core.Tokens;

internal sealed class GetTokenUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<GetTokenUseCase> _logger;

    public GetTokenUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<GetTokenUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<PaginatedList<TokenListItem>>> GetTokens(
        SearchData request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tokens list. Page {PageNumber} Size {PageSize}",
            request.PageNumber, request.PageSize);

        var query = _dbContext.TokensSearch
            .AsNoTracking()
            .Where(t => t.TenantId == _currentUserService.TenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(token =>
                (token.TokenIdHash ?? string.Empty).ToLower().Contains(term) ||
                (token.ClientId ?? string.Empty).ToLower().Contains(term) ||
                (token.ClientName ?? string.Empty).ToLower().Contains(term) ||
                (token.UserName ?? string.Empty).ToLower().Contains(term) ||
                (token.Subject ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var tokenTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "TokenType", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(tokenTypeCriteria?.Value))
        {
            var tokenType = tokenTypeCriteria.Value.Trim();
            query = query.Where(token => token.TokenType == tokenType);
        }

        var clientCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "ClientId", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(clientCriteria?.Value))
        {
            var clientId = clientCriteria.Value.Trim();
            query = query.Where(token => token.ClientId == clientId);
        }

        var userCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "UserId", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(userCriteria?.Value) &&
            int.TryParse(userCriteria.Value, out var userId))
        {
            query = query.Where(token => token.UserId == userId);
        }

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(statusCriteria?.Value) &&
            Enum.TryParse<TokenStatus>(statusCriteria.Value, true, out var status))
        {
            query = query.Where(token => token.Status == status);
        }

        criterias = criterias
            .Where(c =>
                !string.Equals(c.ColumnName, "TokenType", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "ClientId", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "UserId", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var tokens = await query
            .Select(TokenListItem.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} tokens", tokens.TotalCount);

        return ApiResult<PaginatedList<TokenListItem>>.Success(tokens);
    }

    public async Task<ApiResult<TokenDetail>> GetTokenById(
        int tokenId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching token {TokenId}", tokenId);

        var token = await _dbContext.TokensSearch
            .AsNoTracking()
            .Where(t => t.Id == tokenId && t.TenantId == _currentUserService.TenantId)
            .Select(TokenDetail.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Token not found: {TokenId}", tokenId);
            return ApiResult<TokenDetail>.Failure(ApiError.Failure("NotFound",
                "Token not found for the Id {0}".FormatString(tokenId)));
        }

        return ApiResult<TokenDetail>.Success(token);
    }
}