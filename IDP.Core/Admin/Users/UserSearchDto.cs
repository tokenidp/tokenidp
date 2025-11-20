using System.Linq.Expressions;

namespace IDP.Core.Admin.Users;

internal class UserSearchDto
{
    public UserSearchDto(int id,
        int tenantId,
        string fullName,
        string userName,
        string tenantName,
        string status,
        string fullAddress,
        string phoneNumber,
        string email,
        string roles,
        string updatedBy)
    {
        Id = id;
        TenantId = tenantId;
        FullName = fullName;
        UserName = userName;
        TenantName = tenantName;
        Status = status;
        FullAddress = fullAddress;
        PhoneNumber = phoneNumber;
        Email = email;
        Roles = roles;
        UpdatedBy = updatedBy;
    }

    public static Expression<Func<UserSearch, UserSearchDto>> Projection =>
        user => new UserSearchDto(user.Id,
            user.TenantId,
            user.FullName,
            user.UserName,
            user.TenantName,
            user.Status,
            user.FullAddress,
            user.PhoneNumber,
            user.Email,
            user.Roles,
            user.UpdatedBy);

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
