using System.Text.Json.Serialization;

namespace TokenIDP.Core.OAuth.Model;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; private set; } = default!;
    [JsonPropertyName("token_type")]
    public string TokenType { get; private set; } = "Bearer";
    [JsonPropertyName("id_token")]
    public string? IDToken { get; private set; } = default!;
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; private set; } = default!;
    [JsonPropertyName("scope")]
    public string Scope { get; private set; } = default!;
    [JsonPropertyName("expires_in")]
    public int ExpireIn { get; private set; }
    public DateTime ExpireAt { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool? TwoFactorEnabled { get; private set; } = default!;
    public int? UserId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorDescription { get; private set; }

    private TokenResponse() { }


    public static TokenResponse Success(bool twoFactorEnabled)
         => new TokenResponse
         {
             IsSuccess = true,
             TwoFactorEnabled = twoFactorEnabled
         };

    public static TokenResponse Success(
        int? userId,
        string token,
        int expireIn,
        DateTime expiry,
        string? idToken)
        => new TokenResponse
        {
            UserId = userId,
            AccessToken = token,
            IDToken = idToken,
            ExpireAt = expiry,
            IsSuccess = true,
            ExpireIn = expireIn
        };

    public static TokenResponse Failure(string errorCode, string? description = null)
        => new TokenResponse
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorDescription = description
        };

    public void AddRefreshToken(string? refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
