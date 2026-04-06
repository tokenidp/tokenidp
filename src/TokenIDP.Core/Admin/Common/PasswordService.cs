namespace TokenIDP.Core.Admin.Common;

public sealed class PasswordService
{
    private readonly IPasswordHasher<User> _hasher;

    public PasswordService(IPasswordHasher<User> hasher)
    {
        _hasher = hasher;
    }

    public void SetPassword(User user, string plainPassword)
    {
        var hash = _hasher.HashPassword(user, plainPassword);
        user.SetPasswordHash(hash);
        user.RotateSecurityStamp();
    }

    public PasswordVerificationResult Verify(User user, string plainPassword)
    {
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword);
    }
}

