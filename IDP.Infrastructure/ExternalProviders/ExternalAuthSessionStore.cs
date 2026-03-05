using IDP.Domain.AggregateRoots.Authorization;
using IDP.ExternalProviders.Abstractions;

namespace IDP.Infrastructure.ExternalProviders;

internal sealed class ExternalAuthSessionStore : IExternalAuthSessionStore
{
    private readonly ICache _cache;

    public ExternalAuthSessionStore(ICache cache)
    {
        _cache = cache;
    }

    public async Task CreateAsync(ExternalAuthSession session, TimeSpan ttl)
    {
        var key = BuildKey(session.Provider, session.State);

        await _cache.SetAsync(
            key,
            session,
            ttl);
    }

    public async Task<ExternalAuthSession?> GetAsync(
        ExternalProviderTypes provider,
        string state)
    {
        var key = BuildKey(provider, state);

        return await _cache.GetAsync<ExternalAuthSession>(key);
    }

    public async Task RemoveAsync(
        ExternalProviderTypes provider,
        string state)
    {
        var key = BuildKey(provider, state);

        await _cache.RemoveAsync(key);
    }

    private static string BuildKey(ExternalProviderTypes provider, string state)
    {
        return $"external-auth:{provider.ToString().ToLowerInvariant()}:{state}";
    }
}