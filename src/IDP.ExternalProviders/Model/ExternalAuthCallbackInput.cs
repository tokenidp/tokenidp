using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.ExternalProviders.Model;

public sealed record ExternalAuthCallbackInput(
    ExternalProviderTypes Provider,
    string Code,
    string State
);
