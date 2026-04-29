using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;
using TokenIDP.Core.Admin.Tokens;
using TokenIDP.Domain.AggregateRoots.Tokens;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class TokenRepository : ITokenRepository
{
    private const int DefaultTokenLookupClientLimit = 200;

    private readonly ApplicationDbContext _dbContext;
    private readonly IAppLogger<TokenRepository> _logger;

    public TokenRepository(ApplicationDbContext dbContext,
        IAppLogger<TokenRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> CreateToken(Token token)
    {
        _dbContext.Tokens.Add(token);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<Token?> GetToken(byte[] tokenHash)
    {
        var token = await _dbContext.Tokens.FirstOrDefaultAsync(s =>
        !(s.TokenStatus == TokenStatus.Revoked) &&
        s.ExpiresAt > DateTime.UtcNow &&
        (
            (s.RefreshToken != null && s.RefreshToken.TokenHash == tokenHash) ||
            (s.ReferenceToken != null && s.ReferenceToken.TokenHash == tokenHash)
        ));

        return token;
    }

    public async Task<Token?> GetRefreshToken(byte[] tokenHash)
    {
        var refreshToken = await _dbContext.Tokens
            .FirstOrDefaultAsync(t => t.RefreshToken.TokenHash == tokenHash
            && t.TokenStatus != TokenStatus.Revoked);

        return refreshToken;
    }

    public async Task<bool> RemoveOldRefreshTokens(int userId, string ipAddress, int expiry)
    {
        _logger.LogDebug("Removing old refresh tokens for user {UserId}", userId);

        var cutoff = DateTime.UtcNow.AddDays(-expiry);

        var oldTokens = await _dbContext.Tokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff)
            .ToListAsync();

        if (oldTokens.Any())
        {
            foreach (var token in oldTokens)
            {
                token.Revoke(RevokeReason.RefreshReuse.ToString(), ipAddress, userId);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Removed {Count} old refresh tokens for user {UserId}",
                oldTokens.Count, userId);
        }
        else
        {
            _logger.LogDebug("No old refresh tokens to remove for user {UserId}", userId);
        }

        return true;
    }

    public async Task<int> RevokeToken(Token token)
    {
        _dbContext.Tokens.Update(token);

        var id = await _dbContext.SaveChangesAsync();

        return id;
    }

    public async Task<Token?> GetActiveTokenAsync(Guid tokenId, int tenantId, CancellationToken ct)
    {
        return await _dbContext.Tokens
            .Include(t => t.ReferenceToken)
            .Include(t => t.RefreshToken)
            .FirstOrDefaultAsync(t =>
                t.Id == tokenId &&
                t.TenantId == tenantId &&
                t.TokenStatus != TokenStatus.Revoked,
                ct);
    }

    public async Task<PaginatedList<TokenListItem>> SearchTokensAsync(int tenantId, SearchData request, CancellationToken ct)
    {
        var query = _dbContext.TokenSearch
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            if (term.Length >= 3)
            {
                query = query.Where(token =>
                    token.TokenId.ToString().ToLower().Contains(term) ||
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

        return await query
            .Select(TokenListItem.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public Task<TokenDetail?> GetTokenDetailAsync(int tenantId, Guid tokenId, CancellationToken ct)
    {
        return _dbContext.TokenSearch
            .AsNoTracking()
            .Where(t => t.Id == tokenId && t.TenantId == tenantId)
            .Select(TokenDetail.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TokenLookups> GetTokenLookupsAsync(int tenantId, CancellationToken ct)
    {
        var tokenTypes = new List<LookupItem>
        {
            new() { Key = "JWT", Value = "JWT" },
            new() { Key = "Reference", Value = "Reference" },
            new() { Key = "Refresh", Value = "Refresh" }
        };

        var statuses = Enum.GetValues<TokenStatus>()
            .Select(value => new LookupItem
            {
                Key = value.ToString(),
                Value = value.ToString()
            })
            .ToList();

        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .OrderBy(c => c.ClientName)
            .Select(c => new LookupItem
            {
                Key = c.ClientId,
                Value = string.IsNullOrWhiteSpace(c.ClientName)
                    ? c.ClientId
                    : $"{c.ClientName} ({c.ClientId})"
            })
            .Take(DefaultTokenLookupClientLimit)
            .ToListAsync(ct);

        return new TokenLookups
        {
            TokenTypes = tokenTypes,
            Statuses = statuses,
            Clients = clients
        };
    }

    public async Task<int> SaveAsync(Token token, CancellationToken ct)
    {
        _dbContext.Tokens.Update(token);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> RevokeActiveTokensForUserAsync(
        int tenantId,
        int userId,
        string reason,
        string ipAddress,
        int revokedByUserId,
        CancellationToken ct)
    {
        var tokens = await _dbContext.Tokens
            .Where(t => t.TenantId == tenantId &&
                        t.UserId == userId &&
                        t.TokenStatus != TokenStatus.Revoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke(reason, ipAddress, revokedByUserId);
        }

        await _dbContext.SaveChangesAsync(ct);

        return tokens.Count;
    }
}


