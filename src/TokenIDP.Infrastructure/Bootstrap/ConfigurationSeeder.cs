using TokenIDP.Core.Admin.Bootstrap;
using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Infrastructure.Bootstrap;

internal class ConfigurationSeeder : IConfigurationSeeder
{
    public async Task CreateAsync(IApplicationDbContext db,
        int tenantId,
        CreateUpdateConfiguration command,
        CancellationToken ct)
    {
        var createResult = Configuration.Create(
           tenantId,
           command.ConfigKey,
           command.ConfigValue,
           command.ValueType,
           command.Scope,
           command.IsEditable,
           out var configuration);

        db.Configurations.Add(configuration!);

        await db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(IApplicationDbContext db,
        int tenantId,
        string configKey,
        string scope,
        CancellationToken ct)
    {
        Enum.TryParse<ConfigurationScopes>(scope, ignoreCase: true, out var result);

        var isExist = await db.Configurations
            .AsNoTracking()
            .AnyAsync(t => t.TenantId == tenantId
            && t.ConfigKey == configKey
            && t.Scope == result, ct);

        return isExist;
    }
}

