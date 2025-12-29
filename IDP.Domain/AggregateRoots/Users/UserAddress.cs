namespace IDP.Domain.AggregateRoots.Users;

public class UserAddress : BaseEntity
{
    [Required]
    public long UserId { get; private set; }
    [Required]
    public int AddressType { get; private set; }
    [Required, MaxLength(200)]
    public string AddressLine1 { get; private set; }
    [MaxLength(200)]
    public string? AddressLine2 { get; private set; }
    [Required, MaxLength(100)]
    public string City { get; private set; } = null!;
    [MaxLength(100)]
    public string? State { get; private set; }
    [MaxLength(20)]
    public string? PostalCode { get; private set; }
    [Required, MaxLength(100)]
    public string Country { get; private set; } = null!;
    [Required]
    public bool IsActive { get; private set; } = true;
    public virtual User User { get; private set; }
}
