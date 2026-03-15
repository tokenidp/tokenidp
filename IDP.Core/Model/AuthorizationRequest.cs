namespace IDP.Core.Model;

public class AuthorizationRequest
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ResponseType { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
    public string CodeChallenge { get; set; } = default!;
    public string Scopes { get; set; } = default!;
    public string AuthorizationContextId { get; set; } = default!;
    public string CodeChallengeMethod { get; set; } = default!; //Default is SHA256
    public int TenantId { get; set; }
    public bool RememberMe { get; set; }

    public AuthorizationRequest() { }

    private AuthorizationRequest(string clientId,
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

    public static AuthorizationRequest Create(string clientId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        string scopes)
    {
        return new AuthorizationRequest(clientId,
            redirectUri,
            codeChallenge,
            codeChallengeMethod,
            scopes);
    }
}
