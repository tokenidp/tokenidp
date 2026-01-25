namespace IDP.Foundation.Abstractions.Stores;

public interface ITenantStore
{
    Task<bool> CheckTwoFactorEnabled(int tenantId);
}
