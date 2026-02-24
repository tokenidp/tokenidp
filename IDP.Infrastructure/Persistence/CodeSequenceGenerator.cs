namespace IDP.Infrastructure.Persistence;

internal class CodeSequenceGenerator : ICodeSequenceGenerator
{
    private readonly IApplicationDbContext _db;

    public CodeSequenceGenerator(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> NextUserCodeAsync(int tenantId, CancellationToken ct)
    {
        var nextVal = await _db.CodeSequences
        .Where(x => x.TenantId == tenantId && x.SequenceKey == "User")
        .ExecuteUpdateAsync(x =>
            x.SetProperty(p => p.LastValue, p => p.LastValue + 1), ct);

        return nextVal;
    }

    public async Task<int> NextTenantCodeAsync(int tenantId, CancellationToken ct)
    {
        var nextVal = await _db.CodeSequences
        .Where(x => x.TenantId == tenantId && x.SequenceKey == "Tenant")
        .ExecuteUpdateAsync(x =>
            x.SetProperty(p => p.LastValue, p => p.LastValue + 1), ct);

        return nextVal;
    }
}
