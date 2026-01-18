using IDP.Foundation.Contracts;

namespace IDP.Foundation.Abstractions;

public interface IRoleStore
{
    Task<IEnumerable<string>> GetUserRoles(int userId);

    Task<ApiResult<bool>> HasPermission(int userId, string claim);

    Task<ApiResult<bool>> HasRole(int userId, string role);
}
