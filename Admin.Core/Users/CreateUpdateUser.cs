using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Users;

internal class CreateUpdateUser
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string UserName { get; set; }
    [Required]
    [Phone]
    public required string Phone { get; set; }
    [Required]
    public required string Password { get; set; }
    [Required]
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    [Required]
    public required string City { get; set; }
    [Required]
    public required string State { get; set; }
    [Required]
    public required string Zip { get; set; }
    [Required]
    [MinLength(1)]
    public required int[] Roles { get; set; }
}
