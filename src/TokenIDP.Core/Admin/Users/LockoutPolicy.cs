namespace TokenIDP.Core.Admin.Users;

public sealed class LockoutPolicy
{
    private readonly int _maxFailed = 3;
    private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);

    public void OnFailedLogin(User user)
    {
        user.IncrementAccessFailed();

        if (user.AccessFailedCount >= _maxFailed)
        {
            user.LockUntil(DateTimeOffset.UtcNow.Add(_lockoutDuration));
            user.ResetAccessFailed();
        }
    }

    public void OnSuccessfulLogin(User user)
    {
        user.ResetAccessFailed();
    }
}
