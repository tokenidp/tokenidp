namespace TokenIDP.Server.Multitenancy;

internal static class TenantResolutionHttpContextItems
{
    public const string TenantKey = "__tenant_key";
    public const string ResolutionSource = "__tenant_resolution_source";
}
