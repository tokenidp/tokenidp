using IDP.ExternalProviders.Abstractions;
using IDP.ExternalProviders.Model;
using IDP.Foundation.Abstractions.Stores;
using System.Security.Cryptography;

namespace IDP.Infrastructure.ExternalProviders;

public sealed class ExternalIdentityLinkService : IExternalIdentityLinkService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserStore _userStore;
    private readonly ILookupNormalizer _normalizer;
    private readonly ICodeSequenceGenerator _userCodeGenerator;

    public ExternalIdentityLinkService(
        IApplicationDbContext dbContext,
        IUserStore userStore,
        ILookupNormalizer normalizer,
        ICodeSequenceGenerator userCodeGenerator)
    {
        _dbContext = dbContext;
        _userStore = userStore;
        _normalizer = normalizer;
        _userCodeGenerator = userCodeGenerator;
    }

    public async Task<User> FindOrProvisionUserAsync(
        int tenantId,
        ExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var linkedUser = await _dbContext.Users
            .Include(x => x.ExternalLogins)
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.ExternalLogins.Any(l =>
                         l.Provider == identity.Provider &&
                         l.ProviderUserId == identity.ProviderUserId),
                cancellationToken);

        if (linkedUser is not null)
        {
            var login = linkedUser.ExternalLogins.First(
                x => x.Provider == identity.Provider &&
                     x.ProviderUserId == identity.ProviderUserId);

            login.UpdateProfile(identity.Email, identity.DisplayName);
            login.RecordLogin();

            await _userStore.UpdateUser(linkedUser);
            return linkedUser;
        }

        User? user = null;

        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            user = await _dbContext.Users
                .Include(x => x.ExternalLogins)
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId &&
                         x.Email == identity.Email,
                    cancellationToken);
        }

        if (user is not null)
        {
            user.AddExternalLogin(
                identity.Provider,
                identity.ProviderUserId,
                identity.Email,
                identity.DisplayName);

            await _userStore.UpdateUser(user);
            return user;
        }

        return await ProvisionUserAsync(tenantId, identity, cancellationToken);
    }

    private async Task<User> ProvisionUserAsync(
        int tenantId,
        ExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var (firstName, lastName) = SplitName(identity.DisplayName);

        var userName = await EnsureUniqueUserNameAsync(
            tenantId,
            BuildUserName(identity),
            cancellationToken);

        var email = identity.Email ?? $"{identity.ProviderUserId}@external.local";
        var phone = "0000000000";

        var result = User.Create(
            tenantId,
            firstName,
            lastName,
            userName,
            email,
            phone,
            createdBy: 0,
            roles: [],
            out var user);

        if (!result.IsSuccess || user is null)
        {
            throw new InvalidOperationException("Failed to provision user for external identity.");
        }

        user.ApplyIdentityFlags(
            lookoutEnabled: false,
            twoFactorEnabled: false,
            emailConfirmed: identity.EmailVerified,
            phoneNumberConfirmed: false,
            accessFailedCount: 0,
            lookoutEnd: null);

        user.AddExternalLogin(
            identity.Provider,
            identity.ProviderUserId,
            identity.Email,
            identity.DisplayName);

        var nextValue = await _userCodeGenerator
            .NextUserCodeAsync(tenantId, cancellationToken);

        user.GenerateUserCode(nextValue);

        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _userStore.CreateUser(user, randomPassword);

        return user;
    }

    private static (string FirstName, string LastName) SplitName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return ("External", "User");
        }

        var parts = displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return (parts[0], "User");
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string BuildUserName(ExternalIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            var at = identity.Email.IndexOf('@');
            if (at > 0)
            {
                return identity.Email[..at];
            }

            return identity.Email;
        }

        return $"{identity.Provider.ToString().ToLowerInvariant()}_{identity.ProviderUserId}";
    }

    private async Task<string> EnsureUniqueUserNameAsync(
        int tenantId,
        string baseUserName,
        CancellationToken cancellationToken)
    {
        var candidate = baseUserName;
        var suffix = 0;

        while (true)
        {
            var normalized = _normalizer.NormalizeName(candidate);
            var exists = await _dbContext.Users.AnyAsync(
                x => x.TenantId == tenantId && x.NormalizedUserName == normalized,
                cancellationToken);

            if (!exists)
            {
                return candidate;
            }

            suffix++;
            candidate = $"{baseUserName}{suffix}";
        }
    }
}