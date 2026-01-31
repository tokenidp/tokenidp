namespace IDP.Domain.Specifications;

public enum AuthenticationAction
{
    Login,
    Logout,
    MfaChallenge,
    PasswordReset
}
