namespace IDP.ExternalProviders.Model;

public sealed record ExternalChallengeRequest(
    int TenantId,
    string CallbackUrl,
    string State,
    string? Nonce,
    string? CodeVerifier
);
