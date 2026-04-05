namespace IDP.ExternalProviders.Security;

public static class OAuthUrlBuilder
{
    public static string BuildAuthorizeUrl(
        string endpoint,
        IReadOnlyDictionary<string, string?> parameters)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("OAuth authorize endpoint is required.", nameof(endpoint));
        }

        var normalized = parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var query = string.Join("&", normalized.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        if (string.IsNullOrWhiteSpace(query))
        {
            return endpoint;
        }

        var separator = endpoint.Contains('?') ? "&" : "?";
        return $"{endpoint}{separator}{query}";
    }
}