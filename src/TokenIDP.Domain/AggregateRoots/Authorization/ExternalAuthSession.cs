namespace TokenIDP.Domain.AggregateRoots.Authorization;

public sealed record ExternalAuthSession(
    int TenantId,
    int ClientId,
    string AuthorizationContextId,
    ExternalProviderTypes Provider,
    string State,
    string CallbackUrl,
    DateTime CreatedAtUtc,
    string? Nonce,
    string? CodeVerifier
);

