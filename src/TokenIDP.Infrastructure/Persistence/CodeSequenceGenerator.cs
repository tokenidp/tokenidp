using TokenIDP.Core.Abstractions;
using TokenIDP.Domain.AggregateRoots;

namespace TokenIDP.Infrastructure.Persistence;

internal class CodeSequenceGenerator : ICodeSequenceGenerator
{
    private readonly ApplicationDbContext _db;
    private const string UserSequenceKey = "User";
    private const string TenantSequenceKey = "Tenant";

    public CodeSequenceGenerator(ApplicationDbContext db)
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

        if (sequenceKey == TenantSequenceKey)
        {
            var currentMaxTenantCodeValue = await GetCurrentMaxTenantCodeValueAsync(ct);
            await EnsureSequenceAtLeastAsync(tenantId, sequenceKey, currentMaxTenantCodeValue, ct);
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
                _db.ChangeTracker.Clear();
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

    private async Task EnsureSequenceAtLeastAsync(
        int tenantId,
        string sequenceKey,
        int minimumValue,
        CancellationToken ct)
    {
        var updatedRows = await _db.CodeSequences
            .Where(x => x.TenantId == tenantId
                && x.SequenceKey == sequenceKey
                && x.LastValue < minimumValue)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.LastValue, minimumValue), ct);

        if (updatedRows > 0)
        {
            return;
        }

        var exists = await _db.CodeSequences
            .AnyAsync(x => x.TenantId == tenantId && x.SequenceKey == sequenceKey, ct);

        if (exists)
        {
            return;
        }

        _db.CodeSequences.Add(new CodeSequence(tenantId, sequenceKey, minimumValue));
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            await _db.CodeSequences
                .Where(x => x.TenantId == tenantId
                    && x.SequenceKey == sequenceKey
                    && x.LastValue < minimumValue)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(p => p.LastValue, minimumValue), ct);
        }
    }

    private async Task<int> GetCurrentMaxTenantCodeValueAsync(CancellationToken ct)
    {
        var prefix = $"TEN-{DateTime.UtcNow:yyyy}-";
        var tenantCodes = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantCode.StartsWith(prefix))
            .Select(x => x.TenantCode)
            .ToListAsync(ct);

        return tenantCodes
            .Select(code => code.Length > prefix.Length
                && int.TryParse(code[prefix.Length..], out var value)
                    ? value
                    : 0)
            .DefaultIfEmpty(0)
            .Max();
    }
}

