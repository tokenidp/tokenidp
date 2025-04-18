namespace IDP.Service.Model;

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; }
    public string ReasonRevoked { get; set; }
}