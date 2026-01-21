namespace Admin.Core.Tokens;

internal sealed class GetTokenLookupsUseCase
{
    private const int DefaultUserLimit = 200;
    private const int DefaultClientLimit = 200;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<GetTokenLookupsUseCase> _logger;

    public GetTokenLookupsUseCase(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAppLogger<GetTokenLookupsUseCase> logger)
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
            new() { Key = "Access", Value = "Access Token" },
            new() { Key = "Refresh", Value = "Refresh Token" },
            new() { Key = "Reference", Value = "Reference Token" },
            new() { Key = "DeviceCode", Value = "Device Code" },
            new() { Key = "Ciba", Value = "CIBA" }
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

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.TenantId == _currentUserService.TenantId)
            .OrderBy(u => u.UserName)
            .Select(u => new LookupItem
            {
                Key = u.Id.ToString(),
                Value = string.IsNullOrWhiteSpace(u.UserName)
                    ? (u.FirstName + " " + u.LastName).Trim()
                    : u.UserName
            })
            .Take(DefaultUserLimit)
            .ToListAsync(cancellationToken);

        return ApiResult<TokenLookups>.Success(new TokenLookups
        {
            TokenTypes = tokenTypes,
            Statuses = statuses,
            Clients = clients,
            Users = users
        });
    }
}