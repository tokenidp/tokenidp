namespace IDP.Core.Admin.Roles;

public class ClaimDto
{
    public int ClaimId { get; private set; }
    public int? ParentId { get; private set; }
    public int UserId { get; private set; }
    public int Sequence { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimName { get; private set; }
    public string ClaimValue { get; private set; }
    public string Icon { get; private set; }
    public string Url { get; private set; }
    public string RoleName { get; private set; }
    public string ControlType { get; private set; }

    public ClaimDto(int claimId,
        int? parentId,
        int userId,
        int sequence,
        string claimType,
        string claimName,
        string claimValue,
        string icon,
        string url,
        string roleName,
        string controlType)
    {
        ClaimId = claimId;
        ParentId = parentId;
        UserId = userId;
        Sequence = sequence;
        ClaimType = claimType;
        ClaimName = claimName;
        ClaimValue = claimValue;
        Icon = icon;
        Url = url;
        RoleName = roleName;
        ControlType = controlType;
    }
}
