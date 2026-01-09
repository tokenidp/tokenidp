namespace IDP.Foundation.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    string CorrelationId { get; }
    string UserName { get; }
    string BaseUrl { get; }
    string Scopes { get; }
    string[] GetRoles();
}
