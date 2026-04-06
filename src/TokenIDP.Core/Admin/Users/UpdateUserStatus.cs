using System.ComponentModel.DataAnnotations;

namespace TokenIDP.Core.Admin.Users;

internal class UpdateUserStatus
{
    [Required]
    public int Id { get; set; }
    [Required]
    public UserStatus Status { get; set; }
}

