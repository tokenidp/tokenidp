namespace Admin.Core;

public interface ICodeSequenceGenerator
{
    Task<int> NextTenantCodeAsync(int tenantId, CancellationToken ct);
    Task<int> NextUserCodeAsync(int tenantId, CancellationToken ct);
}
