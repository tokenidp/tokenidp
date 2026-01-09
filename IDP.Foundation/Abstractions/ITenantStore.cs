namespace IDP.Foundation.Abstractions;

public interface ITenantStore
{
    Task<bool> CheckTwoFactorEnabled(int tenantId);
}
