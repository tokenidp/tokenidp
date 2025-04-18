namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class Tenant
{
    [Key]
    public int Id { get; private set; }
    public string TenantName { get; private set; }
    public string TenantCode { get; private set; }
    public string Email { get; private set; }
    public bool? IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string HomePageUrl { get; private set; }
    public virtual ICollection<User> Users { get; private set; }
    public virtual ICollection<Role> Roles { get; private set; }
    public virtual ICollection<Client> Clients { get; private set; }

    private Tenant() { }
}
