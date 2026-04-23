namespace TokenIDP.Domain;

public static class SystemIdentity
{
    public const string SystemTenantKey = "system";
    public const string SystemAdminClientId = "idp-admin";

    private static readonly HashSet<string> ReservedTenantKeySet = new(StringComparer.OrdinalIgnoreCase)
    {
        SystemTenantKey,
        "admin",
        "api",
        "auth",
        "login",
        "www",
        "root",
        "app"
    };

    private static readonly HashSet<string> ReservedSystemClientIdSet = new(StringComparer.OrdinalIgnoreCase)
    {
        SystemAdminClientId,
        "admin-client",
        "system-client"
    };

    public static IReadOnlySet<string> ReservedTenantKeys => ReservedTenantKeySet;

    public static IReadOnlySet<string> ReservedSystemClientIds => ReservedSystemClientIdSet;

    public static bool IsReservedTenantKey(string? tenantKey)
        => !string.IsNullOrWhiteSpace(tenantKey) &&
           ReservedTenantKeySet.Contains(tenantKey.Trim());

    public static bool IsReservedSystemClientId(string? clientId)
        => !string.IsNullOrWhiteSpace(clientId) &&
           ReservedSystemClientIdSet.Contains(clientId.Trim());

    public static string GetDefaultOperationalAdminClientId(string tenantKey)
    {
        var normalizedTenantKey = string.IsNullOrWhiteSpace(tenantKey)
            ? "tenant"
            : tenantKey.Trim().ToLowerInvariant();

        return $"{normalizedTenantKey}-admin";
    }
}
