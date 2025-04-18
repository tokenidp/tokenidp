using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class AppClaim : BaseEntity, IAggregateRoot
{
    public int? ParentId { get; private set; }
    public int Sequence { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimName { get; private set; }
    public string AccessUrl { get; private set; }
    public string Icon { get; private set; }
    public string ControlType { get; private set; }
    public bool ShowToTenant { get; private set; }
    public bool IsActive { get; private set; }

    public virtual ICollection<AppClaimTenant> AppClaimTenants { get; private set; }

    private AppClaim() { }

    public AppClaim(int parentId,
        string claimType,
        string claimName,
        string accessUrl,
        string controlType,
        bool showToTenant,
        bool isActive)
    {
        ParentId = parentId;
        ClaimType = claimType;
        ClaimName = claimName;
        AccessUrl = accessUrl;
        ControlType = controlType;
        ShowToTenant = showToTenant;
        IsActive = isActive;
    }

    public void UpdateAppClaim(int parentId,
        string claimType,
        string claimName,
        bool showToTenant,
        bool isActive)
    {
        ParentId = parentId;
        ClaimType = claimType;
        ClaimName = claimName;
        ShowToTenant = showToTenant;
        IsActive = isActive;
    }
}
