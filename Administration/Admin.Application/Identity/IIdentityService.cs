using Identity.Application.Identity.Authentication;

namespace Identity.Application.Identity;

public interface IIdentityService
{
    Task<AuthResponse> Authenticate(string userName, string password);

    Task<AuthResponse> RefreshToken(string refreshToken);
}
