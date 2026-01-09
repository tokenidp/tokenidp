namespace IDP.Domain.AggregateRoots.Users;

public class AuthenticationResult
{
    public bool IsSuccess { get; private set; }
    public int? UserId { get; private set; }
    public bool? TwoFactorEnabled { get; private set; }
    public string Error { get; private set; } = string.Empty;

    private AuthenticationResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private AuthenticationResult(int userId,
       bool twoFactorEnabled)
    {
        IsSuccess = true;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
    }

    public static AuthenticationResult Success(int userId, bool twoFactorEnabled)
    {
        return new AuthenticationResult(userId, twoFactorEnabled);
    }

    public static AuthenticationResult Failure(string error)
    {
        return new AuthenticationResult(false) { Error = error };
    }
}
