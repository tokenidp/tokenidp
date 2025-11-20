namespace IDP.Web.Model;

public class AuthRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string CodeChallenge { get; set; }
    public string Scopes { get; set; }
    public string CodeChallengeMethod { get; set; } //Default is SHA256
}
