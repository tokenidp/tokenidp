namespace IDP.Domain.AggregateRoots.Tenants;

public sealed record OidcClientConfig
{
    public string ClientId { get; init; } = default!;
    public string? ClientSecret { get; init; }
    public Uri Authority { get; init; } = default!;
    public IReadOnlyCollection<string> Scopes { get; init; } = Array.Empty<string>();
    public string CallbackPath { get; init; } = default!;

    public static OidcClientConfig Create(
        string clientId,
        Uri authority,
        IEnumerable<string> scopes,
        string callbackPath,
        string? clientSecret = null)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("ClientId is required.");

        if (authority is null || !authority.IsAbsoluteUri)
            throw new DomainException("Authority must be an absolute URI.");

        if (string.IsNullOrWhiteSpace(callbackPath))
            throw new DomainException("CallbackPath is required.");

        return new OidcClientConfig
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Authority = authority,
            Scopes = scopes?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                     ?? Array.Empty<string>(),
            CallbackPath = callbackPath
        };
    }
}
