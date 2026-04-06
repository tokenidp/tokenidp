namespace TokenIDP.Core.Admin;

public interface ICodeSequenceGenerator
{
    Task<int> NextTenantCodeAsync(int tenantId, CancellationToken ct);
    Task<int> NextUserCodeAsync(int tenantId, CancellationToken ct);
}

