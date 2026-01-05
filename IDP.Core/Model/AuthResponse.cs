namespace IDP.Core.Model;

public class AuthResponse
{
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; }
    public string CorrelationId { get; private set; }
    public string AuthorizationCode { get; private set; }
    public int? UserId { get; private set; }
    public bool? TwoFactorEnabled { get; private set; }

    private AuthResponse(string code)
    {
        IsSuccess = true;
        AuthorizationCode = code;
    }

    private AuthResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private AuthResponse(int userId,
       bool twoFactorEnabled)
    {
        IsSuccess = true;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
    }

    private AuthResponse(int userId,
        string correlatonId,
        bool twoFactorEnabled)
    {
        IsSuccess = true;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
        CorrelationId = correlatonId;
    }

    public static AuthResponse Success(string code)
    {
        return new AuthResponse(code);
    }

    public static AuthResponse Success(int userId, bool twoFactorEnabled)
    {
        return new AuthResponse(userId, twoFactorEnabled);
    }

    public static AuthResponse Success(int userId,
       string correlatonId,
       bool twoFactorEnabled)
    {
        return new AuthResponse(userId, correlatonId, twoFactorEnabled);
    }

    public static AuthResponse Failure(string error)
    {
        return new AuthResponse(false) { Error = error };
    }
}
