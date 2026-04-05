namespace IDP.Core.Abstractions;

public interface IDeviceAuthenticationUseCase
{
    Task<AuthenticationResult> AuthenticateAsync(string userCode, string userName, string password);
    Task<AuthenticationResult> ApproveAsync(string userCode, int userId);
}
