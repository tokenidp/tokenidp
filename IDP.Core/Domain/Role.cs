namespace IDP.Core.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
internal class Role : IdentityRole<int>, IBaseEntity, ITenant, IAggregateRoot, IAuditable
{
    public int TenantId { get; private set; }
    public string RoleDescription { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public bool? IsActive { get; private set; }
    public bool IsEditable { get; private set; } = false;
    public bool? IsDeleted { get; private set; }
    public virtual ICollection<UserRole> UserRoles { get; private set; }
    public virtual ICollection<RolePermission> RolePermissions { get; private set; }
    public virtual Tenant Tenant { get; private set; }

    public Role() : base() { }

    public Role(int tenantId,
        string name,
        string description,
        bool? isActive)
    {
        TenantId = tenantId;
        Name = name;
        RoleDescription = description;
        IsActive = isActive;
    }

    public void UpdateRole(
        string name,
        string description,
        bool? isActive)
    {
        Name = name;
        RoleDescription = description;
        IsActive = isActive;
    }

    public void DeleteRole()
    {
        IsDeleted = true;
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
