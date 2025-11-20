namespace IDP.Core.Model;

public class RefreshTokenRequest
{
    public string ClientId { get; set; }
    public string RefreshToken { get; set; }
}