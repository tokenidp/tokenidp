namespace TokenIDP.Core.OAuth.ExternalProviders.Model;

public sealed record ExternalChallengeRequest(
    int TenantId,
    string CallbackUrl,
    string State,
    string? Nonce,
    string? CodeVerifier
);

