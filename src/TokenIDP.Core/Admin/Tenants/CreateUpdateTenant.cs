namespace TokenIDP.Core.Admin.Tenants;

public sealed class CreateUpdateTenant
{
    public int Id { get; set; }
    public string TenantName { get; set; } = default!;
    public string? Email { get; set; }
    public bool IsActive { get; set; }

    public TenantAuthSettingDetail AuthSettings { get; set; } = default!;
    public TenantUISettingDetail UISetting { get; set; } = default!;
    public List<TenantExternalProviderDetail> Providers { get; set; } = new();

    public string GenerateTenantCode(int value)
    {
        return $"TEN-{DateTime.UtcNow:yyyy}-{value:D6}";
    }
}

public sealed class TenantAuthSettingDetail
{
    public AuthenticationModes AuthenticationMode { get; set; }

    public bool AllowLocalLogin { get; set; }
    public bool RequireEmailVerification { get; set; }
    public bool AllowSelfRegistration { get; set; }

    public bool TwoFactorEnabled { get; set; }
    public int? TwoFactorCodeExpiry { get; set; }
}


public sealed class TenantExternalProviderDetail
{
    public ExternalProviderTypes ProviderType { get; set; }
    public bool Enabled { get; set; }
    public bool HasClientSecret { get; set; }

    public string ClientId { get; set; } = default!;
    public string? ClientSecret { get; set; }
}

public class TenantUISettingDetail
{
    public string? Theme { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string? DefaultLanguage { get; private set; }
    public string? LoginText { get; private set; }
}
