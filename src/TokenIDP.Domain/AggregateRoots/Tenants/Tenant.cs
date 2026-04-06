namespace TokenIDP.Domain.AggregateRoots.Tenants;

public partial class Tenant : AggregateRoot<int>
{
    private readonly List<TenantExternalProvider> _tenantExternalProviders = new();

    public string TenantName { get; private set; } = default!;
    public string? TenantDisplayName { get; private set; }
    public string TenantCode { get; private set; } = default!;
    public string TenantKey { get; private set; } = default!;
    public string? Email { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int EffectiveUserId { get; private set; }

    public virtual TenantAuthSetting TenantAuthSetting { get; private set; } = default!;
    public virtual TenantUISetting TenantUISetting { get; private set; } = default!;
    public IReadOnlyCollection<TenantExternalProvider> TenantExternalProviders => _tenantExternalProviders.AsReadOnly();

    private Tenant() { }

    private Tenant(string tenantName,
        string tenantKey,
        string? email,
        bool isActive,
        TenantAuthSetting authSetting,
        TenantUISetting tenantUISetting)
    {
        TenantName = tenantName;
        TenantKey = tenantKey;
        Email = email;
        IsActive = isActive;

        TenantAuthSetting = authSetting;
        TenantUISetting = tenantUISetting;
    }

    public static Result Create(
        string tenantName,
        string tenantKey,
        string? email,
        bool isActive,
        TenantAuthSetting authSetting,
        TenantUISetting tenantUISetting,
        out Tenant? tenant)
    {
        tenant = null;

        var validation = ValidateInput(tenantName);
        if (!validation.IsSuccess)
            return validation;

        if (authSetting is null)
            return Result.Failure("tenant.authsettings.required", "Tenant auth settings are required.");

        tenant = new Tenant(
            tenantName: tenantName,
            tenantKey: tenantKey,
            email: email,
            isActive: isActive,
            authSetting: authSetting,
            tenantUISetting);

        return Result.Success(0);
    }

    public Result UpdateTenantProfile(
        string tenantName,
        string? email,
        bool isActive)
    {
        var validation = ValidateInput(tenantName);
        if (!validation.IsSuccess)
            return validation;

        TenantName = tenantName;
        Email = email;
        IsActive = isActive;

        return Result.Success(Id);
    }

    public void GenerateTenantCode(int value)
    {
        TenantCode = $"TEN-{DateTime.UtcNow:yyyy}-{value:D6}";
    }

    public Result Disable()
    {
        IsActive = false;
        return Result.Success(Id);
    }

    public Result ConfigureAuthSettings(Action<TenantAuthSetting> configure)
    {
        if (TenantAuthSetting is null)
            return Result.Failure("tenant.authsettings.missing", "Tenant auth settings are missing.");

        configure(TenantAuthSetting);

        return Result.Success(Id);
    }

    public Result EnableTwoFactor(TimeSpan codeExpiry)
        => ConfigureAuthSettings(x => x.EnableTwoFactor(codeExpiry));

    public Result DisableTwoFactor()
        => ConfigureAuthSettings(x => x.DisableTwoFactor());

    public Result SetAuthenticationMode(AuthenticationModes mode)
        => ConfigureAuthSettings(x => x.SetAuthenticationMode(mode));

    public Result SetLocalLoginAllowed(bool allowed)
        => ConfigureAuthSettings(x =>
        {
            if (allowed) x.EnableLocalLogin();
            else x.DisableLocalLogin();
        });

    public Result SetSelfRegistrationAllowed(bool allowed)
        => ConfigureAuthSettings(x =>
        {
            if (allowed) x.EnableSelfRegistration();
            else x.DisableSelfRegistration();
        });

    public Result SetRequireEmailVerification(bool required)
        => ConfigureAuthSettings(x =>
        {
            if (required) x.RequireVerifiedEmail();
            else x.AllowUnverifiedEmail();
        });

    public Result AddExternalProvider(
        ExternalProviderTypes providerType,
        OidcClientConfig config)
    {
        if (_tenantExternalProviders.Any(x => x.ProviderType == providerType))
        {
            return Result.Failure(
                "tenant.externalprovider.exists",
                $"External provider '{providerType}' already exists for this tenant.");
        }

        var provider = TenantExternalProvider.Create(Id, providerType, config);
        _tenantExternalProviders.Add(provider);

        return Result.Success(Id);
    }

    public Result UpdateExternalProviderConfig(
        ExternalProviderTypes providerType,
        OidcClientConfig config)
    {
        var provider = _tenantExternalProviders
            .FirstOrDefault(x => x.ProviderType == providerType);

        if (provider is null)
        {
            return Result.Failure(
                "tenant.externalprovider.notfound",
                $"External provider '{providerType}' not found for this tenant.");
        }

        provider.UpdateOidcConfig(config);
        return Result.Success(Id);
    }

    public Result EnableExternalProvider(ExternalProviderTypes providerType)
    {
        var provider = _tenantExternalProviders
            .FirstOrDefault(x => x.ProviderType == providerType);

        if (provider is null)
        {
            return Result.Failure(
                "tenant.externalprovider.notfound",
                $"External provider '{providerType}' not found for this tenant.");
        }

        provider.Enable();
        return Result.Success(Id);
    }

    public Result DisableExternalProvider(ExternalProviderTypes providerType)
    {
        var provider = _tenantExternalProviders
            .FirstOrDefault(x => x.ProviderType == providerType);

        if (provider is null)
        {
            return Result.Failure(
                "tenant.externalprovider.notfound",
                $"External provider '{providerType}' not found for this tenant.");
        }

        provider.Disable();
        return Result.Success(Id);
    }

    public Result RemoveExternalProvider(ExternalProviderTypes providerType)
    {
        var provider = _tenantExternalProviders
            .FirstOrDefault(x => x.ProviderType == providerType);

        if (provider is null)
        {
            return Result.Failure(
                "tenant.externalprovider.notfound",
                $"External provider '{providerType}' not found for this tenant.");
        }

        _tenantExternalProviders.Remove(provider);
        return Result.Success(Id);
    }

    private static Result ValidateInput(string tenantName)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Result.Failure("tenant.name.invalid", "Tenant name is required.");
        }

        return Result.Success(0);
    }
}
