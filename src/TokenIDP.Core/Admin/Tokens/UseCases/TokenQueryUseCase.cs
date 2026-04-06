using TokenIDP.Core.Admin.Common;
using TokenIDP.Domain.AggregateRoots.Tokens;

namespace TokenIDP.Core.Admin.Tokens.UseCases;

internal sealed class TokenQueryUseCase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TokenQueryUseCase> _logger;

    public TokenQueryUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TokenQueryUseCase> logger)
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

        var query = _dbContext.TokenSearch
            .AsNoTracking()
            .Where(t => t.TenantId == _currentUserService.TenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();

        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            if (term.Length < 3)
            {
                term = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(token =>
                    (token.TokenId.ToString() ?? string.Empty).ToLower().Contains(term) ||
                    (token.ClientId ?? string.Empty).ToLower().Contains(term) ||
                    (token.ClientName ?? string.Empty).ToLower().Contains(term) ||
                    (token.UserName ?? string.Empty).ToLower().Contains(term));
            }
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var sourceTypeCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "SourceType", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(sourceTypeCriteria?.Value))
        {
            var sourceType = sourceTypeCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(token => token.SourceType.ToLower() == sourceType);
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "SourceType", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(statusCriteria?.Value) &&
            Enum.TryParse<TokenStatus>(statusCriteria.Value, true, out var status))
        {
            query = query.Where(token => token.Status == status);
        }

        var tokens = await query
            .Select(TokenListItem.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);

        _logger.LogDebug("Fetched {Count} tokens", tokens.TotalCount);

        return ApiResult<PaginatedList<TokenListItem>>.Success(tokens);
    }

    public async Task<ApiResult<TokenDetail>> GetTokenById(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching token {TokenId}", tokenId);

        var token = await _dbContext.TokenSearch
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

