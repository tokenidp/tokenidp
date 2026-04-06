namespace TokenIDP.Core.OAuth.Model;

public class RevokeTokenRequest
{
    public string Token { get; set; }
    public string ReasonRevoked { get; set; }
    public string IpAddress { get; set; }
}
