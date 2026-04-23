namespace TokenIDP.Core.Foundation.Options;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "TenantResolution";

    public string[] AllowedRootDomains { get; set; } = Array.Empty<string>();
    public string[] SystemHostAliases { get; set; } = Array.Empty<string>();
    public string[] AllowedDevelopmentHosts { get; set; } = new[] { "localhost" };
    public string? DefaultTenant { get; set; }
    public string? FallbackTenantKey
    {
        get => DefaultTenant;
        set
        {
            if (string.IsNullOrWhiteSpace(DefaultTenant))
            {
                DefaultTenant = value;
            }
        }
    }
    public bool AllowQueryInStaging { get; set; } = true;
    public bool AllowHeaderInStaging { get; set; } = true;
    public string QueryParameterName { get; set; } = "tenant";
    public string HeaderName { get; set; } = "X-Tenant-Key";
    public int MaxTenantKeyLength { get; set; } = 64;
    public int LookupCacheMinutes { get; set; } = 15;
    public int InvalidHostThrottleWindowSeconds { get; set; } = 60;
    public int InvalidHostThrottleMaxAttempts { get; set; } = 20;
}
