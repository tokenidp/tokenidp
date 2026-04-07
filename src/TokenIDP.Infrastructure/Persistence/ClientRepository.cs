using TokenIDP.Core.OAuth.Model;
using TokenIDP.Infrastructure.Projections;
using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Clients;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Infrastructure.Persistence;

internal sealed class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICache _cache;
    private readonly IAppLogger<ClientRepository> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ClientRepository(ApplicationDbContext dbContext,
        IAppLogger<ClientRepository> logger,
        ICache cache,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _currentUserService = currentUserService;
    }

    public async Task<ClientValidationSnapshot> GetActiveByClientId(string clientId)
    {
        _logger.LogDebug("GetClient client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("ACT", clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
                .AsNoTracking()
                .Include(x => x.ClientGrantTypes)
                .Include(x => x.ClientScopes)
                .Include(x => x.ClientApiResources)
                .Include(x => x.ClientSecrets)
                .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive);

            if (client == null)
            {
                return null;
            }

            var activeApiResourceNames = client.ClientApiResources
                .Where(x => x.IsActive)
                .Select(x => x.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var apiScopeAssignments = activeApiResourceNames.Length == 0
                ? new List<ClientApiScopeAssignment>()
                : await _dbContext.ApiResources
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == client.TenantId &&
                        x.Enabled &&
                        activeApiResourceNames.Contains(x.Name))
                    .SelectMany(resource => resource.Scopes
                        .Where(scope => scope.Enabled)
                        .Select(scope => new ClientApiScopeAssignment(scope.Name, resource.Name)))
                    .ToListAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return new ClientValidationSnapshot(
                client.ClientId,
                client.ClientName,
                client.TenantId,
                client.IsActive,
                client.RedirectUri,
                client.LogoutRedirectUri,
                client.ClientType,
                client.TokenType,
                client.ClientGrantTypes.Select(g => g.AllowedGrantType),
                client.ClientScopes.Select(s => s.Scope),
                activeApiResourceNames,
                apiScopeAssignments,
                client.ClientSecrets
                    .Where(s => s.ExpiresAt > DateTime.UtcNow && s.IsRevoked != true)
                    .Select(s => s.SecretHash),
                client.AccessTokenLifetime,
                client.AuthorizationCodeLifetime,
                client.RefreshTokenExpiration,
                client.ClientSecretExpiry);
        });

        _logger.LogDebug("Retrieved client {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientShortInfo> GetClientShortInfo(int clientId)
    {
        _logger.LogDebug("GetValidationClient: Checking is valid client for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("SHT", clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
                .Where(x => x.Id == clientId)
                .Select(ClientShortInfoProjection.Projection)
                .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client client validation result for {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientShortInfo> GetClientShortInfo(string clientId)
    {
        _logger.LogDebug("GetValidationClient: Checking is valid client for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("VAL", clientId);

        var clientDto = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.Clients
                .Where(x => x.ClientId == clientId)
                .Select(ClientShortInfoProjection.Projection)
                .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client for {CacheKey}", cacheKey);

            return client;
        });

        _logger.LogDebug("Retrieved client client validation result for {ClientId}", clientId);

        return clientDto ?? throw new NotFoundException("Client not found.");
    }

    public async Task<ClientExpiringSecret> GetClientExpiringSecretsAsync(int daysAhead,
        CancellationToken ct)
    {
        var tenantId = _currentUserService.TenantId;
        var now = DateTime.UtcNow;
        var untilUtc = now.AddDays(daysAhead);

        var cacheKey = CacheKeys.DASHBOARD_EXPIRING_CLIENT_SECRETS
            .FormatCacheKey(tenantId, daysAhead);

        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var expiring = await (
                 from s in _dbContext.ClientSecrets
                 join c in _dbContext.Clients on s.ClientId equals c.Id
                 where c.TenantId == tenantId
                    && s.ExpiresAt <= untilUtc
                    && c.IsActive
                 group new { s, c } by new { s.ClientId, c.ClientName } into g
                 select new
                 {
                     g.Key.ClientId,
                     g.Key.ClientName,
                     ExpiresAtUtc = g.Min(x => x.s.ExpiresAt)
                 }
             ).ToListAsync(ct);

            var items = expiring.Select(x => new ClientExpiringSecretItem
            {
                ClientId = x.ClientId,
                ClientName = x.ClientName,
                ExpiresAtUtc = x.ExpiresAtUtc,
                DaysLeft = (int)Math.Ceiling((x.ExpiresAtUtc - now).TotalDays)
            }).ToList();

            return new ClientExpiringSecret
            {
                ExpiringClientCount = items.Count,
                Clients = items
                .OrderBy(x => x.DaysLeft)
                .ToList()
            };
        },
        expiration: TimeSpan.FromMinutes(15));
    }

    public Task<ClientDetail?> GetClientDetailAsync(int tenantId, int clientId, CancellationToken ct)
    {
        return _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.Id == clientId && c.TenantId == tenantId)
            .Select(ClientDetail.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaginatedList<ClientDetail>> SearchClientsAsync(int tenantId, SearchData request, CancellationToken ct)
    {
        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        var criterias = request.SearchCriterias?.ToList() ?? new List<SearchCriteria>();
        var searchCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(searchCriteria?.Value))
        {
            var term = searchCriteria.Value.Trim().ToLowerInvariant();
            query = query.Where(client =>
                (client.ClientName ?? string.Empty).ToLower().Contains(term) ||
                (client.ClientId ?? string.Empty).ToLower().Contains(term));
        }

        criterias = criterias
            .Where(c => !string.Equals(c.ColumnName, "Search", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statusCriteria = criterias.FirstOrDefault(c =>
            string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase));
        if (statusCriteria != null)
        {
            criterias = criterias
                .Where(c => !string.Equals(c.ColumnName, "IsActive", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (bool.TryParse(statusCriteria.Value, out var isActive))
            {
                query = query.Where(client => client.IsActive == isActive);
            }
        }

        return await query
            .Select(ClientDetail.Projection)
            .ApplyFilter(criterias)
            .ApplySort(request.SortColumn, request.SortOrder)
            .ToPaginatedListAsync(request.PageNumber, request.PageSize, request.SearchAll);
    }

    public async Task<ClientLookups> GetClientLookupsAsync(int tenantId, CancellationToken ct)
    {
        var apiResources = await _dbContext.ApiResources
            .AsNoTracking()
            .Where(resource => resource.TenantId == tenantId && resource.Enabled)
            .OrderBy(resource => resource.DisplayName)
            .Select(resource => new ApiResourceLookup
            {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Scopes = resource.Scopes
                    .Where(scope => scope.Enabled)
                    .OrderBy(scope => scope.DisplayName)
                    .Select(scope => new ApiScopeLookup
                    {
                        Id = scope.Id,
                        Name = scope.Name,
                        DisplayName = scope.DisplayName
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var externalProviders = await _dbContext.TenantExternalProviders
            .AsNoTracking()
            .Where(provider => provider.TenantId == tenantId)
            .OrderBy(provider => provider.ProviderType)
            .Select(provider => new LookupItem
            {
                Key = provider.Id.ToString(),
                Value = provider.ProviderType.ToString()
            })
            .ToListAsync(ct);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId && !role.IsDeleted)
            .OrderBy(role => role.Name)
            .Select(role => new LookupItem
            {
                Key = role.Id.ToString(),
                Value = role.Name ?? string.Empty
            })
            .ToListAsync(ct);

        return new ClientLookups
        {
            AppTypes = ClientLookupMapper.MapAppTypes(),
            TokenTypes = ClientLookupMapper.MapTokenTypes(),
            ClientScopes = ClientLookupMapper.MapClientScopes(),
            ApiResources = apiResources,
            GrantTypes = ClientLookupMapper.MapGrantTypes(),
            ExternalProviders = externalProviders,
            Roles = roles
        };
    }

    public Task<bool> ClientIdExistsAsync(int tenantId, string clientId, CancellationToken ct)
    {
        return _dbContext.Clients
            .AsNoTracking()
            .AnyAsync(
                c => c.TenantId == tenantId &&
                     c.ClientId.ToLower() == clientId.ToLower(),
                ct);
    }

    public async Task<IEnumerable<ClientExternalProviderSnapshot>> GetExternalProviders(int clientId)
    {
        _logger.LogDebug("GetExternalProviders: Get external providers for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("EPRV", clientId);

        var externalProviders = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await (from cp in _dbContext.ClientExternalProviders.AsNoTracking()
                                join tp in _dbContext.TenantExternalProviders.AsNoTracking()
                                on cp.ExternalProviderId equals tp.Id
                                where cp.ClientId == clientId
                                && tp.OidcConfig != null
                                select new ClientExternalProviderSnapshot(
                                    tp.ProviderType.ToString(),
                                    cp.EnabledForClient,
                                    tp.Enabled,
                                    tp.OidcConfig!.ClientId,
                                    tp.OidcConfig.ClientSecret
                                )).ToListAsync();

            _logger.LogDebug("Cached client external providers for {CacheKey}", cacheKey);

            return client;
        }, expiration: TimeSpan.FromMinutes(30));

        _logger.LogDebug("Retrieved external providers for client: {ClientId}", clientId);

        return externalProviders;
    }

    public async Task<ClientAuthPolicy?> GetClientAuthPolicy(int clientId)
    {
        _logger.LogDebug("GetClientAuthPolicy: Get auth policy for client: {ClientId}", clientId);

        var cacheKey = CacheKeys.CLIENT.FormatCacheKey("AUTH", clientId);

        var authPolicy = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var client = await _dbContext.ClientAuthPolicies
                .Where(s => s.ClientId == clientId)
                .FirstOrDefaultAsync();

            _logger.LogDebug("Cached client auth policy for {CacheKey}", cacheKey);

            return client;
        }, expiration: TimeSpan.FromMinutes(2));

        _logger.LogDebug("Retrieved auth policy for client: {ClientId}", clientId);

        return authPolicy;
    }

    public async Task<Client?> GetClientAggregateAsync(int clientId, int tenantId, CancellationToken ct)
    {
        return await _dbContext.Clients
            .Include(c => c.ClientScopes)
            .Include(c => c.ClientGrantTypes)
            .Include(c => c.ClientApiResources)
            .Include(c => c.ClientAuthPolicy)
            .Include(c => c.ClientExternalProviders)
            .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId, ct);
    }

    public async Task<int> AddAsync(Client client, CancellationToken ct)
    {
        _dbContext.Clients.Add(client);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteAsync(Client client, CancellationToken ct)
    {
        _dbContext.Clients.Remove(client);
        return await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LookupItem>> GetTokenClientLookupsAsync(int tenantId, int limit, CancellationToken ct)
    {
        return await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.ClientName)
            .Select(c => new LookupItem
            {
                Key = c.ClientId,
                Value = string.IsNullOrWhiteSpace(c.ClientName)
                    ? c.ClientId
                    : $"{c.ClientName} ({c.ClientId})"
            })
            .Take(limit)
            .ToListAsync(ct);
    }
}

