using Admin.Core.Users;

namespace IDP.Infrastructure.Persistence;

internal class UserCodeGenerator : IUserCodeGenerator
{
    private readonly IApplicationDbContext _db;

    public UserCodeGenerator(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> GenerateNextValueAsync(int tenantId, CancellationToken ct)
    {
        var nextVal = await _db.UserCodeSequences
        .Where(x => x.TenantId == tenantId)
        .ExecuteUpdateAsync(x =>
            x.SetProperty(p => p.LastValue, p => p.LastValue + 1), ct);

        return nextVal;
    }
}
