namespace TokenIDP.Core.Foundation.Options;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "TenantResolution";

    public string[] AllowedRootDomains { get; set; } = Array.Empty<string>();
    public string[] AllowedDevelopmentHosts { get; set; } = new[] { "localhost" };
    public string? FallbackTenantKey { get; set; }
    public int LookupCacheMinutes { get; set; } = 15;
    public int InvalidHostThrottleWindowSeconds { get; set; } = 60;
    public int InvalidHostThrottleMaxAttempts { get; set; } = 20;
}
