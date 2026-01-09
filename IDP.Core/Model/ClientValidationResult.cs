namespace IDP.Core.Model;

public class ClientValidationResult
{
    public bool IsValidClient { get; private set; }
    public string RedirectUri { get; private set; }
    public IReadOnlySet<string> Scopes { get; private set; }

    private ClientValidationResult(bool isValidClient, string redirectUri, IReadOnlySet<string> scopes)
    {
        IsValidClient = isValidClient;
        Scopes = scopes;
        RedirectUri = redirectUri;
    }

    public static ClientValidationResult Create(bool isValidClient, string redirectUri, IReadOnlySet<string> scopes)
    {
        return new ClientValidationResult(isValidClient, redirectUri, scopes);
    }
}
