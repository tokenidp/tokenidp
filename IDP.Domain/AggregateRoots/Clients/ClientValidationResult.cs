using IDP.Domain.Specifications;

namespace IDP.Core.Model;

public class ClientValidationResult
{
    public bool IsValidClient { get; private set; }
    public string RedirectUri { get; private set; }
    public IReadOnlySet<string> Scopes { get; private set; }
    public IReadOnlySet<GrantTypes> GrantTypes { get; private set; }

    public ClientValidationResult(string redirectUri,
        IEnumerable<string> scopes,
        IEnumerable<GrantTypes> grantTypes)
    {
        IsValidClient = true;
        Scopes = scopes.ToHashSet();
        RedirectUri = redirectUri;
        GrantTypes = grantTypes.ToHashSet();
    }
}
