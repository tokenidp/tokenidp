namespace TokenIDP.Server.Multitenancy;

public enum TenantHostResolutionKind
{
    None = 0,
    Invalid = 1,
    Tenant = 2,
    Root = 3
}
