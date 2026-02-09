using IDP.Domain.AggregateRoots.Configurations;

namespace IDP.Foundation.Abstractions.Stores;

public interface IConfigurationStore
{
    Task<IEnumerable<ConfigurationShortInfo>> GetTenantConfigurations(int tenantId, ConfigurationScopes type);
}
