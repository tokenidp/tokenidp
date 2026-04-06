using TokenIDP.Domain.AggregateRoots.Tokens;

namespace TokenIDP.Core.Admin.Tokens.UseCases;

internal sealed class TokenLookupsUseCase
{
    private const int DefaultClientLimit = 200;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<TokenLookupsUseCase> _logger;

    public TokenLookupsUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<TokenLookupsUseCase> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResult<TokenLookups>> GetLookups(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching token lookups for tenant {TenantId}", _currentUserService.TenantId);

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
            .Where(c => c.TenantId == _currentUserService.TenantId)
            .OrderBy(c => c.ClientName)
            .Select(c => new LookupItem
            {
                Key = c.ClientId,
                Value = string.IsNullOrWhiteSpace(c.ClientName)
                    ? c.ClientId
                    : $"{c.ClientName} ({c.ClientId})"
            })
            .Take(DefaultClientLimit)
            .ToListAsync(cancellationToken);

        return ApiResult<TokenLookups>.Success(new TokenLookups
        {
            TokenTypes = tokenTypes,
            Statuses = statuses,
            Clients = clients
        });
    }
}
