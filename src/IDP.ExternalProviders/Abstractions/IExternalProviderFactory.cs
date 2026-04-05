using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.ExternalProviders.Abstractions;

public interface IExternalProviderFactory
{
    IExternalProviderClient Get(ExternalProviderTypes provider);
}

