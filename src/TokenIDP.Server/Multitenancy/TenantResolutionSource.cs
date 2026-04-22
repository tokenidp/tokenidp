namespace TokenIDP.Server.Multitenancy;

public enum TenantResolutionSource
{
    Host = 1,
    Query = 2,
    Header = 3,
    Default = 4
}
