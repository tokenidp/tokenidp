namespace IDP.Service.Model;

public class TokenRequest
{
    public int UserId { get; set; }
    public string ClientId { get; set; }
    public string GrantType { get; set; } // always authorization_code
    public string Code { get; set; }
    public string CodeVerifier { get; set; }
    public string RedirectUri { get; set; }
}