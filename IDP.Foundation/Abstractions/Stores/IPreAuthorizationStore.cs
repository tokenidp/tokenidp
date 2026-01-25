using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Foundation.Abstractions.Stores;

public interface IPreAuthorizationStore
{
    Task<PreAuthorization?> GetPreAuthorization(string correlationId, int userId);

    Task<int> Create(PreAuthorization preAuthorization);

    Task<int> Update(PreAuthorization preAuthorization);
}
