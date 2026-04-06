namespace TokenIDP.Core.OAuth.Model;

public class AuthenticationResult
{
    public bool IsSuccess { get; init; }
    public int UserId { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public string CorrelationId { get; private set; } = default!;
    public string? Error { get; init; }

    public static AuthenticationResult Success(int userId,
        bool twoFactorEnabled,
        string correlationId = "")
         => new AuthenticationResult
         {
             IsSuccess = true,
             TwoFactorEnabled = twoFactorEnabled,
             UserId = userId,
             CorrelationId = correlationId
         };

    public static AuthenticationResult Failure(string error)
        => new AuthenticationResult
        {
            IsSuccess = false,
            Error = error,
        };
}

