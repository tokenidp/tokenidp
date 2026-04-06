using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Foundation.Abstractions.Stores;

public interface IConfigurationStore
{
    Task<IEnumerable<ConfigurationShortInfo>> GetTenantConfigurations(int tenantId, ConfigurationScopes type);
}

