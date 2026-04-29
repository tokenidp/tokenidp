using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Core.Admin.Common;

namespace TokenIDP.Core.Admin.Clients.UseCases;

internal sealed class ClientLookupsUseCase
{
    private readonly ICache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<ClientLookupsUseCase> _logger;
    private readonly IClientRepository _clientRepository;

    public ClientLookupsUseCase(
        ICache cache,
        ICurrentUserService currentUserService,
        IAppLogger<ClientLookupsUseCase> logger,
        IClientRepository clientRepository)
    {
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
        _clientRepository = clientRepository;
    }

    public async Task<ApiResult<ClientLookups>> GetClientLookups(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeys.LOOKUP}:client:{_currentUserService.TenantId}";

        _logger.LogDebug("Fetching client lookups for tenant {TenantId}", _currentUserService.TenantId);

        var lookups = await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                return await _clientRepository.GetClientLookupsAsync(
                    _currentUserService.TenantId,
                    cancellationToken);
            }, TimeSpan.FromMinutes(15));

        _logger.LogDebug("Client lookups fetched for tenant {TenantId}", _currentUserService.TenantId);

        return ApiResult<ClientLookups>.Success(lookups);
    }
}
