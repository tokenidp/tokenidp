namespace IDP.Domain.AggregateRoots.Tenants;

public sealed record OidcClientConfig
{
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }

    public static OidcClientConfig Create(
        string clientId,
        string? clientSecret = null)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("ClientId is required.");

        return new OidcClientConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };
    }
}