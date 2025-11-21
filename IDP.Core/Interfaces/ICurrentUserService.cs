namespace IDP.Core.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    string UserName { get; }
    string[] GetRoles();
}
