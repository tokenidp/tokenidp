namespace IDP.Core.Model;

public class AuthorizationResponse
{
    public bool IsSuccess { get; private set; } = default!;
    public string Error { get; private set; } = default!;
    public string CorrelationId { get; private set; } = default!;
    public string AuthorizationCode { get; private set; } = default!;
    public int? UserId { get; private set; } = default!;
    public bool? TwoFactorEnabled { get; private set; } = default!;

    private AuthorizationResponse(string code)
    {
        IsSuccess = true;
        AuthorizationCode = code;
    }

    private AuthorizationResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private AuthorizationResponse(int userId,
       bool twoFactorEnabled)
    {
        IsSuccess = true;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
    }

    private AuthorizationResponse(int userId,
        string correlatonId,
        bool twoFactorEnabled)
    {
        IsSuccess = true;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
        CorrelationId = correlatonId;
    }

    public static AuthorizationResponse Success(string code)
    {
        return new AuthorizationResponse(code);
    }

    public static AuthorizationResponse Success(int userId, bool twoFactorEnabled)
    {
        return new AuthorizationResponse(userId, twoFactorEnabled);
    }

    public static AuthorizationResponse Success(int userId,
       string correlatonId,
       bool twoFactorEnabled)
    {
        return new AuthorizationResponse(userId, correlatonId, twoFactorEnabled);
    }

    public static AuthorizationResponse Failure(string error)
    {
        return new AuthorizationResponse(false) { Error = error };
    }
}
