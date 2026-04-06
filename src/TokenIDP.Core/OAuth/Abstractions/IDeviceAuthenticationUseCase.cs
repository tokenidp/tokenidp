namespace TokenIDP.Core.OAuth.Abstractions;

public interface IDeviceAuthenticationUseCase
{
    Task<Model.AuthenticationResult> AuthenticateAsync(string userCode, string userName, string password);
    Task<Model.AuthenticationResult> ApproveAsync(string userCode, int userId);
}

