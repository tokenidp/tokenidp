using IDP.Domain.AggregateRoots.Authorization;

namespace IDP.Foundation.Abstractions.Stores;

public interface IAuthorizationCodeStore
{
    Task<int> Create(AuthorizationCode authorizationCode);

    Task<int> Update(AuthorizationCode authorizationCode);

    Task<AuthorizationCode?> GetByCode(string code);
}
