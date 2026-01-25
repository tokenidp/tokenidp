using IDP.Domain.AggregateRoots.Lookups;

namespace IDP.Foundation.Abstractions.Stores;

public interface ILookupStore
{
    Task<IEnumerable<LookupValue>> GeTenantLookupsByType(int tenantId, string type);
}
