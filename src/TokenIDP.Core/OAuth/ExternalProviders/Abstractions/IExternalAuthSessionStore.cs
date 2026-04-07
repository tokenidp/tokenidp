using TokenIDP.Domain.AggregateRoots.Authorization;
using TokenIDP.Domain.AggregateRoots.Tenants;

namespace TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

public interface IExternalAuthSessionStore
{
    Task CreateAsync(ExternalAuthSession session, TimeSpan ttl);

    Task<ExternalAuthSession?> GetAsync(
        ExternalProviderTypes provider,
        string state);

    Task RemoveAsync(
        ExternalProviderTypes provider,
        string state);
}

