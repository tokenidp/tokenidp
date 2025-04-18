namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class Role : IdentityRole<int>
{
    public int TenantId { get; private set; }
    public string RoleDescription { get; private set; }
    public bool? IsActive { get; private set; }
    public bool ShowToTenant { get; private set; } = false;
    public bool? IsDeleted { get; private set; }
    public virtual ICollection<UserRole> UserRoles { get; private set; }
    public virtual ICollection<RolePermission> RolePermissions { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public Role() : base() { }

}
