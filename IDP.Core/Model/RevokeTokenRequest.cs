namespace IDP.Core.Model;

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; }
    public string ReasonRevoked { get; set; }
    public string IpAddress { get; set; }
}