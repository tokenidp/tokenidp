using TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

namespace TokenIDP.Infrastructure.ExternalProviders;

internal sealed class ExternalProviderFactory : IExternalProviderFactory
{
    private readonly IReadOnlyDictionary<ExternalProviderTypes, IExternalProviderClient> _providers;

    public ExternalProviderFactory(IEnumerable<IExternalProviderClient> providers)
    {
        _providers = providers.ToDictionary(x => x.Provider);
    }

    public IExternalProviderClient Get(ExternalProviderTypes provider)
    {
        if (_providers.TryGetValue(provider, out var client))
        {
            return client;
        }

        throw new InvalidOperationException($"Provider '{provider}' is not registered.");
    }
}


