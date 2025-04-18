namespace Services.Common.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int TenantId { get; }
    string UserName { get; }
    string[] GetRoles();
}
