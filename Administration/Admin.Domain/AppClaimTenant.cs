using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public partial class AppClaimTenant : BaseEntity, ITenant
{
    public int TenantId { get; private set; }
    public int AppClaimId { get; private set; }
    public virtual ICollection<AppRoleClaim> AppRoleClaims { get; private set; }
    public virtual Tenant Tenant { get; private set; }
    public virtual AppClaim AppClaim { get; private set; }

    private AppClaimTenant() { }

    public AppClaimTenant(int appClaimId, string claimType)
    {
        AppClaimId = appClaimId;
        ClaimType = claimType;
    }
}

public partial class AppClaimTenant
{
    public string ClaimType { get; private set; }
}
