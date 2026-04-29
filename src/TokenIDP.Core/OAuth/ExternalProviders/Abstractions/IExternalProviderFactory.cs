namespace TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

public interface IExternalProviderFactory
{
    IExternalProviderClient Get(ExternalProviderTypes provider);
}


