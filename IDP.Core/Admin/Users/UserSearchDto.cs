using System.Linq.Expressions;

namespace IDP.Core.Admin.Users;

internal class UserSearchDto
{
    internal static Expression<Func<UserSearch, UserSearchDto>> Projection =>
        user => new UserSearchDto()
        {
            Id = user.Id,
            TenantId = user.TenantId,
            FullName = user.FullName,
            UserName = user.UserName,
            TenantName = user.TenantName,
            Status = user.Status,
            FullAddress = user.FullAddress,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Roles = user.Roles,
            UpdatedBy = user.UpdatedBy
        };

    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string FullName { get; private set; }
    public string UserName { get; private set; }
    public string TenantName { get; private set; }
    public string Status { get; private set; }
    public string FullAddress { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Email { get; private set; }
    public string Roles { get; private set; }
    public string UpdatedBy { get; private set; }
}
