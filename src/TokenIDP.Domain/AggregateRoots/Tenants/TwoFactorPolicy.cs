namespace TokenIDP.Domain.AggregateRoots.Tenants;

public sealed record TwoFactorPolicy
{
    public bool IsEnabled { get; init; }
    public TimeSpan? CodeExpiry { get; init; }

    public static TwoFactorPolicy Disabled() =>
        new() { IsEnabled = false, CodeExpiry = null };

    public static TwoFactorPolicy Enabled(TimeSpan codeExpiry)
    {
        if (codeExpiry <= TimeSpan.Zero)
            throw new DomainException("Two-factor code expiry must be positive.");

        return new TwoFactorPolicy
        {
            IsEnabled = true,
            CodeExpiry = codeExpiry
        };
    }
}

