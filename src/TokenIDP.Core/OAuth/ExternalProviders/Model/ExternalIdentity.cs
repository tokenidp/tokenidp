using TokenIDP.Domain.AggregateRoots.Tenants;

namespace TokenIDP.Core.OAuth.ExternalProviders.Model;

public sealed record ExternalIdentity(
    ExternalProviderTypes Provider,
    string ProviderUserId,
    string? Email,
    string? DisplayName,
    bool EmailVerified,
    IReadOnlyDictionary<string, string> Claims
);

