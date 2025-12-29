using static IDP.Domain.User;

namespace Admin.Core.Users;

internal class UpdateUserStatus
{
    public int Id { get; set; }
    public UserStatus Status { get; set; }
}