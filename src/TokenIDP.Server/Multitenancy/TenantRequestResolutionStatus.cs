namespace TokenIDP.Server.Multitenancy;

public enum TenantRequestResolutionStatus
{
    Resolved = 1,
    Missing = 2,
    InvalidHost = 3,
    InvalidTenantKey = 4
}
