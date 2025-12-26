namespace IDP.Core.Common.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    string CorrelationId { get; }
    string UserName { get; }
    string BaseUrl { get; }
    string[] GetRoles();
}
