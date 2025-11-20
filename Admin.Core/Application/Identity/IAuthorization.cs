namespace Identity.Application.Identity;

public interface IAuthorization
{
    Task<bool> IsInRole(int userId, string role);

    Task<bool> IsAuthorized(int userId, string claim);
}
