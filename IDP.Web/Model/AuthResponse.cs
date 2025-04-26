using System.Text.Json.Serialization;

namespace IDP.Web.Model;

public class AuthResponse
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
    public string CorrelationId { get; set; }
    public string AuthorizationCode { get; set; }
    public int? UserId { get; set; }
    public bool? TwoFactorEnabled { get; set; }

    [JsonConstructor]
    private AuthResponse() { }

    private AuthResponse(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        IsSuccess = false;
        Error = message;
    }

    public static AuthResponse Failure(string message)
    {
        return new AuthResponse(message);
    }
}
