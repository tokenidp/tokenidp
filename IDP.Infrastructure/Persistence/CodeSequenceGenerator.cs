using IDP.Domain.AggregateRoots;

namespace IDP.Infrastructure.Persistence;

internal class CodeSequenceGenerator : ICodeSequenceGenerator
{
    private readonly IApplicationDbContext _db;
    private const string UserSequenceKey = "User";
    private const string TenantSequenceKey = "Tenant";

    public CodeSequenceGenerator(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> NextUserCodeAsync(int tenantId, CancellationToken ct)
    {
        return await NextValueAsync(tenantId, UserSequenceKey, ct);
    }

    public async Task<int> NextTenantCodeAsync(int tenantId, CancellationToken ct)
    {
        return await NextValueAsync(tenantId, TenantSequenceKey, ct);
    }

    private async Task<int> NextValueAsync(int tenantId, string sequenceKey, CancellationToken ct)
    {
        if (tenantId <= 0)
        {
            throw new InvalidOperationException(
                $"Invalid tenant id '{tenantId}' for sequence '{sequenceKey}'.");
        }

        var updatedRows = await _db.CodeSequences
            .Where(x => x.TenantId == tenantId && x.SequenceKey == sequenceKey)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.LastValue, p => p.LastValue + 1), ct);

        if (updatedRows == 0)
        {
            _db.CodeSequences.Add(new CodeSequence(tenantId, sequenceKey, 0));
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Another request might have created the row concurrently.
            }

            updatedRows = await _db.CodeSequences
                .Where(x => x.TenantId == tenantId && x.SequenceKey == sequenceKey)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(p => p.LastValue, p => p.LastValue + 1), ct);

            if (updatedRows == 0)
            {
                throw new InvalidOperationException(
                    $"Unable to initialize sequence '{sequenceKey}' for tenant {tenantId}.");
            }
        }

        return await _db.CodeSequences
            .Where(x => x.TenantId == tenantId && x.SequenceKey == sequenceKey)
            .Select(x => x.LastValue)
            .SingleAsync(ct);
    }
}