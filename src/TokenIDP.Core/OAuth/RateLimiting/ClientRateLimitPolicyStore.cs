using TokenIDP.Core.Abstractions;
using TokenIDP.Core.Abstractions.Repositories;
using TokenIDP.Domain.AggregateRoots.Clients;

namespace TokenIDP.Core.OAuth.RateLimiting;

internal interface IClientRateLimitPolicyStore
{
    ValueTask<ClientRateLimitProfile?> GetAsync(string clientId, CancellationToken cancellationToken);
}

internal sealed class ClientRateLimitPolicyStore : IClientRateLimitPolicyStore
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IClientRepository _clientRepository;
    private readonly ICache _cache;

    public ClientRateLimitPolicyStore(IClientRepository clientRepository, ICache cache)
    {
        _clientRepository = clientRepository;
        _cache = cache;
    }

    public async ValueTask<ClientRateLimitProfile?> GetAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var normalizedClientId = clientId.Trim();
        var cacheKey = $"oauth-rate-limit:{normalizedClientId}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _clientRepository.FindRateLimitProfileAsync(normalizedClientId, cancellationToken),
            CacheDuration);
    }
}
