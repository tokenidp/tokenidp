namespace TokenIDP.Domain.AggregateRoots.Users.Enums;

public enum AuthenticationAction
{
    Login,
    Logout,
    MfaChallenge,
    PasswordReset
}

