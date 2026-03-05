using IDP.Domain.AggregateRoots.Authorization;
using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.ExternalProviders.Abstractions;

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
