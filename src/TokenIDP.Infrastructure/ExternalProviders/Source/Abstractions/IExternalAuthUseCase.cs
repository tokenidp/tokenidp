using TokenIDP.Domain.AggregateRoots.Tenants;
using TokenIDP.Core.OAuth.ExternalProviders.Model;

namespace TokenIDP.Core.OAuth.ExternalProviders.Abstractions;

public interface IExternalAuthUseCase
{
    Task<ExternalChallengeResult> StartChallengeAsync(
        ExternalProviderTypes provider,
        string authorizationContextId,
        CancellationToken cancellationToken);

    Task<ExternalCallbackResult> HandleCallbackAsync(
        ExternalAuthCallbackInput input,
        CancellationToken cancellationToken);
}
