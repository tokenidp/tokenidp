namespace IDP.Domain.AggregateRoots.Users;

public class UserContact
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int UserId { get; set; }
    [MaxLength(50)]
    public string? Relationship { get; set; } // e.g., Parent, Spouse, Guardian
    [Required, MaxLength(50)]
    public string ContactType { get; set; } = null!; // e.g., Email, Mobile, WorkPhone
    [MaxLength(256)]
    public string? Email { get; set; }
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }
    [MaxLength(200)]
    public string? AddressLine1 { get; set; }
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    [MaxLength(100)]
    public string? Country { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public virtual User User { get; set; }
}

