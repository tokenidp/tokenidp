namespace IDP.Domain.AggregateRoots.Users;

public sealed class AuthenticationContext
{
    public User User { get; }
    public int UserId { get; }
    public bool PasswordVerified { get; }
    public bool IsSuccess { get; }

    public string Error { get; private set; } = string.Empty;

    private AuthenticationContext()
    {
        User = default!;
        IsSuccess = false;
    }

    private AuthenticationContext(User user, bool passwordVerified)
    {
        User = user;
        UserId = user.Id;
        IsSuccess = true;
        PasswordVerified = passwordVerified;
    }

    public static AuthenticationContext Authenticated(User user)
        => new(user, true);

    public bool RequiresMfa(bool tenantMfaEnabled)
        => PasswordVerified && IsTenantMfaEligible(tenantMfaEnabled);

    private bool IsTenantMfaEligible(bool tenantMfaEnabled)
        => tenantMfaEnabled && User.TwoFactorEnabled;

    public bool IsUserLocked()
        => User.LockoutEnd.HasValue &&
           User.LockoutEnd > DateTimeOffset.UtcNow;

    public static AuthenticationContext Failure(string error)
    {
        return new() { Error = error };
    }
}
