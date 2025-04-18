namespace IDP.Service.Model;

public class TokenResponse
{
    public int UserId { get; private set; }
    public string AccessToken { get; private set; }
    public string IDToken { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime Expiry { get; private set; }

    private TokenResponse()
    {

    }

    public static TokenResponse Create(int userId, string token,
        DateTime expiry,
        string refreshToken = "",
        string idToken = "")
    {
        return new TokenResponse()
        {
            UserId = userId,
            AccessToken = token,
            IDToken = idToken,
            RefreshToken = refreshToken,
            Expiry = expiry
        };
    }
}