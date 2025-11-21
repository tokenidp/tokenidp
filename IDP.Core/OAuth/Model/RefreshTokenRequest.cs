namespace IDP.Core.OAuth.Model;

public class RefreshTokenRequest
{
    public string ClientId { get; set; }
    public string RefreshToken { get; set; }
}