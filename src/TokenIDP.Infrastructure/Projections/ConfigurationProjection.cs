using TokenIDP.Core.Admin.Configurations;
using TokenIDP.Domain.AggregateRoots.Configurations;
using System.Linq.Expressions;

namespace TokenIDP.Infrastructure.Projections;

internal static class ConfigurationProjection
{
    public static Expression<Func<Configuration, ConfigurationShortInfo>> ProjectionShort =>
        config => new ConfigurationShortInfo
        (
            config.ConfigKey,
            config.ConfigValue,
            config.ValueType
        );

    public static Expression<Func<Configuration, ConfigurationDetail>> Projection =>
        t => new ConfigurationDetail
    (
        t.Id,
        t.TenantId,
        t.ConfigKey,
        t.ConfigValue,
        t.ValueType,
        t.Scope.ToString(),
        t.IsEditable
 );
}

