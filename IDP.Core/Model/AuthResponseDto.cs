namespace IDP.Core.Model;

public class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
    public string CorrelationId { get; set; }
    public string AuthorizationCode { get; set; }
    public int? UserId { get; set; }
    public bool? TwoFactorEnabled { get; set; }

    private AuthResponseDto(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public static AuthResponseDto Failure(string error)
    {
        return new AuthResponseDto(false) { Error = error };
    }
}
