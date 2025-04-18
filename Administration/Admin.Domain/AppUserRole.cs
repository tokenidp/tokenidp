using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class AppUserRole : IdentityUserRole<int>, IBaseEntity, IAuditable
{
    public int Id { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public virtual AppRole AppRole { get; private set; }
    public virtual AppUser AppUser { get; private set; }

    public AppUserRole() : base() { }

    public AppUserRole(int roleId)
    {
        RoleId = roleId;
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
