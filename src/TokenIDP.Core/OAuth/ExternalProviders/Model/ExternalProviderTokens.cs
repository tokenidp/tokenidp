namespace TokenIDP.Core.OAuth.ExternalProviders.Model;

public sealed record ExternalProviderTokens(
    string AccessToken,
    string? RefreshToken,
    string? IdToken,
    int ExpiresInSeconds,
    string TokenType
);

