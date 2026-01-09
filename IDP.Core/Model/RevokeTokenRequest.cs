namespace IDP.Core.Model;

public class RevokeTokenRequest
{
    public int UserId { get; set; }
    public string Token { get; set; }
    public string ReasonRevoked { get; set; }
    public string IpAddress { get; set; }
}