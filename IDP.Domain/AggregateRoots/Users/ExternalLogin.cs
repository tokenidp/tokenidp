namespace IDP.Domain.AggregateRoots.Users;

public sealed class ExternalLogin : Entity<int>
{
    public int UserId { get; private set; }

    public ExternalProviderTypes Provider { get; private set; } = default!;
    public string ProviderUserId { get; private set; } = default!;

    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public User User { get; private set; } = default!;

    private ExternalLogin() { } // EF

    private ExternalLogin(
        int userId,
        ExternalProviderTypes provider,
        string providerUserId,
        string? email,
        string? displayName)
    {
        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new DomainException("Provider user id cannot be empty.");

        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId.Trim();

        Email = email?.Trim();
        DisplayName = displayName?.Trim();

        CreatedAtUtc = DateTime.UtcNow;
    }

    public static ExternalLogin Create(
        int userId,
        ExternalProviderTypes provider,
        string providerUserId,
        string? email,
        string? displayName)
    {
        return new ExternalLogin(
            userId,
            provider,
            providerUserId,
            email,
            displayName);
    }

    public void UpdateProfile(string? email, string? displayName)
    {
        Email = email?.Trim();
        DisplayName = displayName?.Trim();
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }
}
