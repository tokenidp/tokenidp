using TokenIDP.Domain.DomainEvents.Tenants;

namespace TokenIDP.Domain.AggregateRoots.Tenants;

public partial class Tenant : AggregateRoot<int>
{
    private static readonly HashSet<string> ReservedTenantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "system",
        "admin",
        "api",
        "auth",
        "login",
        "www",
        "root",
        "app"
    };

    private readonly List<TenantExternalProvider> _tenantExternalProviders = new();

    public string TenantName { get; private set; } = default!;
    public string? TenantDisplayName { get; private set; }
    public string TenantCode { get; private set; } = default!;
    public string TenantKey { get; private set; } = default!;
    public string? Email { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsSystemTenant { get; private set; }
    public bool IsDeleted { get; private set; }
    public int EffectiveUserId { get; private set; }

    public virtual TenantAuthSetting TenantAuthSetting { get; private set; } = default!;
    public virtual TenantUISetting TenantUISetting { get; private set; } = default!;
    public IReadOnlyCollection<TenantExternalProvider> TenantExternalProviders => _tenantExternalProviders.AsReadOnly();

    private Tenant() { }

    private Tenant(string tenantName,
        string tenantKey,
        string? email,
        bool isActive,
        bool isSystemTenant,
        TenantAuthSetting authSetting,
        TenantUISetting tenantUISetting)
    {
        TenantName = tenantName;
        TenantKey = tenantKey;
        Email = email;
        IsActive = isActive;
        IsSystemTenant = isSystemTenant;
        IsDeleted = false;

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
        bool isSystemTenant,
        out Tenant? tenant)
    {
        tenant = null;

        var validation = ValidateInput(tenantName, tenantKey, isSystemTenant);
        if (!validation.IsSuccess)
            return validation;

        if (authSetting is null)
            return Result.Failure("tenant.authsettings.required", "Tenant auth settings are required.");

        tenant = new Tenant(
            tenantName: tenantName,
            tenantKey: NormalizeTenantKey(tenantKey),
            email: email,
            isActive: isActive,
            isSystemTenant: isSystemTenant,
            authSetting: authSetting,
            tenantUISetting);

        return Result.Success(0);
    }

    public Result UpdateTenantProfile(
        string tenantName,
        string? email,
        bool isActive)
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

        var validation = ValidateName(tenantName);
        if (!validation.IsSuccess)
            return validation;

        TenantName = tenantName;
        Email = email;

        if (isActive)
            Activate();
        else
            Disable();

        return Result.Success(Id);
    }

    public void GenerateTenantCode(int value)
    {
        TenantCode = $"TEN-{DateTime.UtcNow:yyyy}-{value:D6}";
    }

    public Result Activate()
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be activated.");
        }

        if (IsActive)
        {
            return Result.Success(Id);
        }

        IsActive = true;
        AddDomainEvent(new TenantActivatedEvent(Id, TenantKey, IsSystemTenant));

        return Result.Success(Id);
    }

    public Result Disable()
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

        if (!IsActive)
        {
            return Result.Success(Id);
        }

        IsActive = false;
        AddDomainEvent(new TenantInactivatedEvent(Id, TenantKey, IsSystemTenant));

        return Result.Success(Id);
    }

    public Result Rename(string tenantName, string tenantKey)
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

        var validation = ValidateInput(tenantName, tenantKey, IsSystemTenant);
        if (!validation.IsSuccess)
            return validation;

        TenantName = tenantName.Trim();
        TenantKey = NormalizeTenantKey(tenantKey);

        return Result.Success(Id);
    }

    public Result EnsureSystemIdentity(string tenantName, string tenantKey)
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

        var validation = ValidateInput(tenantName, tenantKey, true);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        TenantName = tenantName.Trim();
        TenantKey = NormalizeTenantKey(tenantKey);
        IsSystemTenant = true;

        return Result.Success(Id);
    }

    public Result SoftDelete()
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Tenant is already deleted.");
        }

        IsDeleted = true;
        IsActive = false;

        return Result.Success(Id);
    }

    public Result ConfigureAuthSettings(Action<TenantAuthSetting> configure)
    {
        if (IsDeleted)
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");

        if (TenantAuthSetting is null)
            return Result.Failure("tenant.authsettings.missing", "Tenant auth settings are missing.");

        configure(TenantAuthSetting);

        return Result.Success(Id);
    }

    public Result UpdateBranding(
        string? theme,
        string? logo,
        string? primaryColor,
        string? defaultLanguage,
        string? loginText)
    {
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

        if (TenantUISetting is null)
        {
            return Result.Failure("tenant.ui.missing", "Tenant UI settings are missing.");
        }

        TenantUISetting.Update(theme, logo, primaryColor, defaultLanguage, loginText);
        AddDomainEvent(new TenantBrandingChangedEvent(Id, TenantKey));

        return Result.Success(Id);
    }

    public void MarkProvisioned()
    {
        AddDomainEvent(new TenantCreatedEvent(Id, TenantKey, IsSystemTenant));
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
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

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
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

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
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

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
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

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
        if (IsDeleted)
        {
            return Result.Failure("tenant.deleted", "Deleted tenant cannot be modified.");
        }

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

    private static Result ValidateInput(string tenantName, string tenantKey, bool isSystemTenant)
    {
        return ValidateName(tenantName)
            .Combine(ValidateTenantKey(tenantKey, isSystemTenant));
    }

    private static Result ValidateName(string tenantName)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Result.Failure("tenant.name.invalid", "Tenant name is required.");
        }

        return Result.Success(0);
    }

    private static Result ValidateTenantKey(string tenantKey, bool isSystemTenant)
    {
        var normalizedTenantKey = NormalizeTenantKey(tenantKey);

        if (string.IsNullOrWhiteSpace(normalizedTenantKey))
        {
            return Result.Failure("tenant.key.invalid", "Tenant key is required.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(
                normalizedTenantKey,
                "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return Result.Failure("tenant.key.invalid", "Tenant key format is invalid.");
        }

        if (isSystemTenant && !string.Equals(normalizedTenantKey, "system", StringComparison.Ordinal))
        {
            return Result.Failure("tenant.key.invalid", "System tenant key must be 'system'.");
        }

        if (!isSystemTenant && ReservedTenantKeys.Contains(normalizedTenantKey))
        {
            return Result.Failure("tenant.key.reserved", "Tenant key is reserved.");
        }

        return Result.Success(0);
    }

    private static string NormalizeTenantKey(string tenantKey)
        => tenantKey.Trim().ToLowerInvariant();
}
