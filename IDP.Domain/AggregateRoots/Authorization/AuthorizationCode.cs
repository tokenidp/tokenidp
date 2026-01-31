namespace IDP.Domain.AggregateRoots.Authorization;
public class AuthorizationCode : AggregateRoot<int>
{
    public string Code { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public int UserId { get; private set; }
    public DateTime Expiry { get; private set; }
    public string RedirectUri { get; private set; } = string.Empty;
    public string CodeChallenge { get; private set; } = string.Empty;
    public string CodeChallengeMethod { get; private set; } = string.Empty; //Default is SHA-256
    public string? Scopes { get; private set; }
    public bool IsUsed { get; private set; }

    private AuthorizationCode() { }

    public AuthorizationCode(string code,
        string codeChallenge,
        string codeChallengeMethod,
        string clientId,
        int userId,
        DateTime expiry,
        string redirectUri,
        string? scopes = null)
    {
        CodeChallenge = codeChallenge;
        Code = code;
        ClientId = clientId;
        UserId = userId;
        Expiry = expiry;
        RedirectUri = redirectUri;
        Scopes = scopes;
        CodeChallengeMethod = codeChallengeMethod;
    }

    public void UpdateIsUsed(bool isUsed)
    {
        IsUsed = isUsed;
    }
}
