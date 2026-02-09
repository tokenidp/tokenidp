namespace IDP.Core.Model;

public class ClientShortInfo
{
    public int Id { get; private set; }
    public string ClientName { get; private set; }
    public bool IsValidClient { get; private set; }
    public string RedirectUri { get; private set; }
    public IReadOnlySet<string> Scopes { get; private set; }
    public IReadOnlySet<GrantTypes> GrantTypes { get; private set; }

    public ClientShortInfo(int id,
        string clientName,
        string redirectUri,
        IEnumerable<string> scopes,
        IEnumerable<GrantTypes> grantTypes)
    {
        Id = id;
        ClientName = clientName;
        IsValidClient = true;
        Scopes = scopes.ToHashSet();
        RedirectUri = redirectUri;
        GrantTypes = grantTypes.ToHashSet();
    }
}
