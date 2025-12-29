namespace IDP.Domain.AggregateRoots.Roles;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class RolePermission : IdentityRoleClaim<int>, IBaseEntity, IAuditable
{
    public int TenantPermissionId { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public virtual Role Role { get; private set; }
    public virtual TenantPermission TenantPermission { get; private set; }

    public RolePermission() : base() { }

    public RolePermission(int tenantPermissionId,
        int roleId,
        string claimType,
        string claimValue)
    {
        TenantPermissionId = tenantPermissionId;
        RoleId = roleId;
        ClaimType = claimType;
        ClaimValue = claimValue;
    }

    public void SetCreatedByAndCreatedOn(int userId)
    {
        CreatedOn = DateTime.UtcNow;
        CreatedBy = userId;
    }

    public void SetUpdatedByAndUpdatedOn(int userId)
    {
        UpdatedOn = DateTime.UtcNow;
        UpdatedBy = userId;
    }
}
