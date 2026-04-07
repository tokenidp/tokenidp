using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Abstractions.Repositories;

public interface IConfigurationRepository
{
    Task<IEnumerable<ConfigurationShortInfo>> GetTenantConfigurations(int tenantId, ConfigurationScopes type);
}

