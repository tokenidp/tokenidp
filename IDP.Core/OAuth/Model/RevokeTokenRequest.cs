namespace IDP.Core.OAuth.Model;

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; }
    public string ReasonRevoked { get; set; }
}