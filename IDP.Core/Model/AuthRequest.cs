namespace IDP.Core.Model;

public class AuthRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string CodeChallenge { get; set; }
    public string Scopes { get; set; }
    public string CodeChallengeMethod { get; set; } //Default is SHA256

    public AuthRequest() { }

    private AuthRequest(string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        string scopes)
    {
        ClientId = clientId;
        RedirectUri = redirectUri;
        CodeChallenge = codeChallenge;
        CodeChallengeMethod = codeChallengeMethod;
        Scopes = scopes;
    }

    public static AuthRequest Create(string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        string scopes)
    {
        return new AuthRequest(clientId,
            redirectUri,
            codeChallenge,
            codeChallengeMethod,
            scopes);
    }
}
