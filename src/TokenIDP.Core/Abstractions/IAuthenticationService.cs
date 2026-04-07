namespace TokenIDP.Core.Abstractions;

public interface IAuthenticationService
{
    Task<AuthenticationContext> Authenticate(int tenantId,
        string userName,
        string password);
}
