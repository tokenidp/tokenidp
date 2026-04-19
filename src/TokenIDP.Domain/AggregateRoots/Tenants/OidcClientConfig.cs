namespace TokenIDP.Domain.AggregateRoots.Tenants;

public sealed record OidcClientConfig
{
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
    public string? Scopes { get; init; }

    public static OidcClientConfig Create(
        string clientId,
        string? clientSecret = null,
        string? scopes = null)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("ClientId is required.");

        return new OidcClientConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = string.IsNullOrWhiteSpace(scopes) ? null : scopes.Trim()
        };
    }
}
