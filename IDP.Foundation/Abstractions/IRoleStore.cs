namespace IDP.Foundation.Abstractions;

public interface IRoleStore
{
    Task<IEnumerable<string>> GetUserRoles(int userId);
}
