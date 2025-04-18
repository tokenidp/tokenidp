using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class AppRoleClaim : IdentityRoleClaim<int>, IBaseEntity, IAuditable
{
    public int AppClaimTenantId { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedOn { get; private set; }
    public virtual AppRole AppRole { get; private set; }
    public virtual AppClaimTenant AppClaimTenant { get; private set; }

    public AppRoleClaim() : base() { }

    public AppRoleClaim(int appClaimTenantId,
        int roleId,
        string claimType,
        string claimValue)
    {
        AppClaimTenantId = appClaimTenantId;
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
