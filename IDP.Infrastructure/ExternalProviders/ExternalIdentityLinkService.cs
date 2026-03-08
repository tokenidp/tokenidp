using IDP.ExternalProviders.Abstractions;
using IDP.ExternalProviders.Model;
using IDP.Foundation.Abstractions.Stores;
using System.Security.Cryptography;

namespace IDP.Infrastructure.ExternalProviders;

internal sealed class ExternalIdentityLinkService : IExternalIdentityLinkService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IClientStore _clientStore;
    private readonly IUserStore _userStore;
    private readonly IAppLogger<ExternalIdentityLinkService> _logger;
    private readonly ICodeSequenceGenerator _userCodeGenerator;
    private readonly ExternalProviderConfigurationResolver _providerConfigurationResolver;

    public ExternalIdentityLinkService(
        IApplicationDbContext dbContext,
        IClientStore clientStore,
        IUserStore userStore,
        IAppLogger<ExternalIdentityLinkService> logger,
        ICodeSequenceGenerator userCodeGenerator,
        ExternalProviderConfigurationResolver providerConfigurationResolver)
    {
        _dbContext = dbContext;
        _clientStore = clientStore;
        _userStore = userStore;
        _logger = logger;
        _userCodeGenerator = userCodeGenerator;
        _providerConfigurationResolver = providerConfigurationResolver;
    }

    public async Task<User> FindOrProvisionUserAsync(
        int tenantId,
        int clientId,
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

            _logger.LogInfo(
                "ExternalUserLinked: TenantId={TenantId}, UserId={UserId}, Provider={Provider}, ProviderUserId={ProviderUserId}",
                tenantId,
                linkedUser.Id,
                identity.Provider,
                identity.ProviderUserId);
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
            var externalLogin = user.AddExternalLogin(
                identity.Provider,
                identity.ProviderUserId,
                identity.Email,
                identity.DisplayName);

            externalLogin?.RecordLogin();

            await _userStore.UpdateUser(user);

            _logger.LogInfo(
                "ExternalUserLinked: TenantId={TenantId}, UserId={UserId}, Provider={Provider}, ProviderUserId={ProviderUserId}",
                tenantId,
                user.Id,
                identity.Provider,
                identity.ProviderUserId);
            return user;
        }

        return await ProvisionUserAsync(tenantId, clientId, identity, cancellationToken);
    }

    private async Task<User> ProvisionUserAsync(
        int tenantId,
        int clientId,
        ExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var providerConfiguration = await _providerConfigurationResolver.ResolveAsync(
            tenantId,
            clientId,
            identity.Provider,
            cancellationToken);

        if (providerConfiguration is null)
        {
            throw new InvalidOperationException("External provider is not configured for this client.");
        }

        var authPolicy = await _clientStore.GetClientAuthPolicy(clientId);
        if (authPolicy is null)
        {
            throw new InvalidOperationException("Client authentication policy is not configured.");
        }

        if (!authPolicy.AutoCreateUsers)
        {
            _logger.LogInfo(
                "ExternalUserPendingApproval: ClientId={ClientId}, Provider={Provider}, ProviderUserId={ProviderUserId}",
                clientId,
                identity.Provider,
                identity.ProviderUserId);

            throw new InvalidOperationException("User provisioning disabled");
        }

        if (!authPolicy.DefaultRoleId.HasValue)
        {
            throw new InvalidOperationException(
                $"Default role is not configured for client {clientId}.");
        }

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
            roles: new[] { authPolicy.DefaultRoleId.Value },
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

        var login = user.AddExternalLogin(
            identity.Provider,
            identity.ProviderUserId,
            identity.Email,
            identity.DisplayName);

        login?.RecordLogin();

        var nextValue = await _userCodeGenerator
            .NextUserCodeAsync(tenantId, cancellationToken);

        user.GenerateUserCode(nextValue);

        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _userStore.CreateUser(user, randomPassword);

        _logger.LogInfo(
            "ExternalUserCreated: TenantId={TenantId}, UserId={UserId}, Provider={Provider}, RoleId={RoleId}",
            tenantId,
            user.Id,
            identity.Provider,
            authPolicy.DefaultRoleId.Value);

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
            var exists = await _dbContext.Users.AnyAsync(
                x => x.TenantId == tenantId && x.UserName == candidate,
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