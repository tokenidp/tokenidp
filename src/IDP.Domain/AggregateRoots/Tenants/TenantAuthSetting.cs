namespace IDP.Domain.AggregateRoots.Tenants;

public enum AuthenticationModes
{
    Local,
    External,
    Mixed
}

public class TenantAuthSetting : Entity<int>
{
    private TenantAuthSetting() { }

    public int TenantId { get; private set; }

    public bool AllowLocalLogin { get; private set; }
    public bool RequireEmailVerification { get; private set; }
    public bool AllowSelfRegistration { get; private set; }

    public AuthenticationModes AuthenticationMode { get; private set; }

    public TwoFactorPolicy TwoFactor { get; private set; } = TwoFactorPolicy.Disabled();

    public virtual Tenant Tenant { get; private set; } = default!;

    public static TenantAuthSetting Create(int tenantId)
    {
        return new TenantAuthSetting
        {
            TenantId = tenantId,
            AllowLocalLogin = true,
            RequireEmailVerification = true,
            AllowSelfRegistration = false,
            AuthenticationMode = AuthenticationModes.Mixed,
            TwoFactor = TwoFactorPolicy.Disabled()
        };
    }

    public void EnableLocalLogin()
    {
        AllowLocalLogin = true;
        EnsureAuthenticationModeConsistency();
    }

    public void DisableLocalLogin()
    {
        if (AuthenticationMode == AuthenticationModes.Local)
            throw new DomainException("Cannot disable local login when AuthenticationMode is LocalOnly.");

        AllowLocalLogin = false;
    }

    public void RequireVerifiedEmail()
    {
        RequireEmailVerification = true;
    }

    public void AllowUnverifiedEmail()
    {
        RequireEmailVerification = false;
    }

    public void EnableSelfRegistration()
    {
        AllowSelfRegistration = true;
    }

    public void DisableSelfRegistration()
    {
        AllowSelfRegistration = false;
    }

    public void SetAuthenticationMode(AuthenticationModes mode)
    {
        AuthenticationMode = mode;
        EnsureAuthenticationModeConsistency();
    }

    public void EnableTwoFactor(TimeSpan codeExpiry)
    {
        TwoFactor = TwoFactorPolicy.Enabled(codeExpiry);
    }

    public void DisableTwoFactor()
    {
        TwoFactor = TwoFactorPolicy.Disabled();
    }

    private void EnsureAuthenticationModeConsistency()
    {
        if (AuthenticationMode == AuthenticationModes.Local && !AllowLocalLogin)
            throw new DomainException("LocalOnly mode requires AllowLocalLogin = true.");

        if (AuthenticationMode == AuthenticationModes.External && AllowLocalLogin)
            throw new DomainException("ExternalOnly mode requires AllowLocalLogin = false.");
    }
}
