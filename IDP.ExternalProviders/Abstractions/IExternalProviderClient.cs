using IDP.Domain.AggregateRoots.Tenants;
using IDP.ExternalProviders.Model;

namespace IDP.ExternalProviders.Abstractions;

public interface IExternalProviderClient
{
    ExternalProviderTypes Provider { get; }

    string BuildAuthorizeUrl(ExternalChallengeRequest request);

    Task<ExternalProviderTokens> ExchangeCodeAsync(
        ExternalCallbackRequest request,
        CancellationToken cancellationToken);

    Task<ExternalIdentity> GetIdentityAsync(
        ExternalProviderTokens tokens,
        CancellationToken cancellationToken);
}
