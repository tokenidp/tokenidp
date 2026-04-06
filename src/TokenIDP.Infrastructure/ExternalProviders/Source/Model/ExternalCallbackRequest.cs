namespace TokenIDP.Core.OAuth.ExternalProviders.Model;

public sealed record ExternalCallbackRequest(
    int TenantId,
    string CallbackUrl,
    string Code,
    string State,
    string? CodeVerifier,
    string? Nonce
);


