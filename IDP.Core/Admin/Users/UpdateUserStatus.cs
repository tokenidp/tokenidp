using static IDP.Core.Domain.User;

namespace IDP.Core.Admin.Users;

internal class UpdateUserStatus
{
    public int Id { get; set; }
    public UserStatus Status { get; set; }
}