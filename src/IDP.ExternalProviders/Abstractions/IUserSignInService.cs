namespace IDP.ExternalProviders.Abstractions;

public interface IUserSignInService
{
    Task SignInAsync(
        int userId,
        string userName,
        string email,
        int tenantId,
        bool rememberMe,
        CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
