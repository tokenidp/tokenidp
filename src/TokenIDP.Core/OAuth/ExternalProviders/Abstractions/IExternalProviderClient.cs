using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Core.OAuth.ExternalProviders.Model;

namespace TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

public interface IExternalProviderClient
{
    ExternalProviderTypes Provider { get; }

    string BuildAuthorizeUrl(ExternalChallengeRequest request);

    Task<ExternalProviderTokens> ExchangeCodeAsync(
        ExternalCallbackRequest request,
        CancellationToken cancellationToken);

    Task<ExternalIdentity> GetIdentityAsync(
        ExternalProviderTokens tokens,
        ExternalCallbackRequest request,
        CancellationToken cancellationToken);
}

