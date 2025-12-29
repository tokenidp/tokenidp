using IDP.Domain.AggregateRoots;

namespace IDP.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public partial class TenantPermission : BaseEntity, ITenant
{
    public int TenantId { get; private set; }
    public int PermissionId { get; private set; }
    public virtual ICollection<RolePermission> RolePermissions { get; private set; }
    public virtual Tenant Tenant { get; private set; }
    public virtual Permission Permission { get; private set; }

    private TenantPermission() { }

    public TenantPermission(int permissionId, string claimType)
    {
        PermissionId = permissionId;
        ClaimType = claimType;
    }
}

public partial class TenantPermission
{
    public string ClaimType { get; private set; }
}
