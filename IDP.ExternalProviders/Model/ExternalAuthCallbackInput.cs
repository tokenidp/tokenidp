using IDP.Domain.AggregateRoots.Tenants;

namespace IDP.ExternalProviders.Model;

public sealed record ExternalAuthCallbackInput(
    int TenantId,
    int ClientId,
    ExternalProviderTypes Provider,
    string Code,
    string State
);
