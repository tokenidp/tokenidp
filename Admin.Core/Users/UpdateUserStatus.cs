using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Users;

internal class UpdateUserStatus
{
    [Required]
    public int Id { get; set; }
    [Required]
    public UserStatus Status { get; set; }
}
