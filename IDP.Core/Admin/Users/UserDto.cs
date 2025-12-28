using System.Linq.Expressions;

namespace IDP.Core.Admin.Users;

internal class UserDto
{
    internal static Expression<Func<User, UserDto>> Projection =>
    user => new UserDto()
    {
        Id = user.Id,
        TenantId = user.TenantId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        StatusId = user.StatusId.ToString(),
        UserName = user.UserName,
        Phone = user.PhoneNumber,
        Roles = user.UserRoles.Select(s => s.RoleId).ToArray()
    };

    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string StatusId { get; private set; } = string.Empty;
    public string? UserName { get; private set; }
    public string? Phone { get; private set; }
    public string Address1 { get; private set; } = string.Empty;
    public string? Address2 { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Zip { get; private set; } = string.Empty;
    public int[] Roles { get; private set; } = new int[0];
}
