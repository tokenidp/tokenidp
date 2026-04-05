using IDP.Domain.AggregateRoots.Tenants;
using IDP.ExternalProviders.Model;

namespace IDP.ExternalProviders.Abstractions;

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