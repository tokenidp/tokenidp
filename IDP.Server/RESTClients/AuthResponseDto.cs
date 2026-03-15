using IDP.Domain.AggregateRoots.Tokens;
using System.Text.Json.Serialization;

namespace IDP.Server.RESTClients;

public class AuthResponseDto
{
    public bool IsSuccess { get; set; } = default!;
    public string Error { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public string AuthorizationCode { get; set; } = default!;
    public int? UserId { get; set; } = default!;
    public bool? TwoFactorEnabled { get; set; } = default!;

    private AuthResponseDto(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    [JsonConstructor] // Tells the library: "Use THIS one"
    public AuthResponseDto(bool isSuccess, string error, string correlationId, string authorizationCode, int? userId, bool? twoFactorEnabled)
    {
        IsSuccess = isSuccess;
        Error = error;
        CorrelationId = correlationId;
        AuthorizationCode = authorizationCode;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
    }

    public static AuthResponseDto Failure(string error)
    {
        return new AuthResponseDto(false) { Error = error };
    }
}
