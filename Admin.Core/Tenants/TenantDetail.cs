namespace Admin.Core.Tenants;

public sealed class TenantDetail
{
    internal static Expression<Func<Tenant, TenantDetail>> Projection =>
        t => new TenantDetail
        {
            Id = t.Id,
            TenantCode = t.TenantCode,
            TenantName = t.TenantName,
            Email = t.Email,
            IsActive = t.IsActive,

            AuthSettings = new TenantAuthSettingDetail
            {
                AuthenticationMode = t.TenantAuthSetting.AuthenticationMode,
                AllowLocalLogin = t.TenantAuthSetting.AllowLocalLogin,
                RequireEmailVerification = t.TenantAuthSetting.RequireEmailVerification,
                AllowSelfRegistration = t.TenantAuthSetting.AllowSelfRegistration,
                TwoFactorEnabled = t.TenantAuthSetting.TwoFactor.IsEnabled,
                TwoFactorCodeExpiry = t.TenantAuthSetting.TwoFactor.CodeExpiry.HasValue
                                            ? (int?)t.TenantAuthSetting.TwoFactor.CodeExpiry.Value.TotalMinutes
        : null
            },

            Providers = t.TenantExternalProviders
                .Select(p => new TenantExternalProviderDetail
                {
                    ProviderType = p.ProviderType,
                    Enabled = p.Enabled,

                    ClientId = p.OidcConfig != null ? p.OidcConfig.ClientId : string.Empty,
                    ClientSecret = p.OidcConfig != null ? p.OidcConfig.ClientSecret : null,
                    Authority = p.OidcConfig != null ? p.OidcConfig.Authority.ToString() : string.Empty,
                    Scopes = p.OidcConfig != null
                        ? string.Join(" ", p.OidcConfig.Scopes)
                        : string.Empty,
                    CallbackPath = p.OidcConfig != null ? p.OidcConfig.CallbackPath : string.Empty
                })
                .ToList()
        };

    public int Id { get; set; }
    public string TenantCode { get; set; } = default!;
    public string TenantName { get; set; } = default!;
    public string? Email { get; set; }
    public bool IsActive { get; set; }

    public TenantAuthSettingDetail AuthSettings { get; set; } = default!;
    public TenantUISettingDetail UISetting { get; set; } = default!;
    public List<TenantExternalProviderDetail> Providers { get; set; } = new();

}