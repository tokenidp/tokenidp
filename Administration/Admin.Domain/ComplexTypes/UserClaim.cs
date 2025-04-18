using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class UserClaim
{
    public int Id { get; private set; }
    public int? ParentId { get; private set; }
    public int UserId { get; private set; }
    public int Sequence { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimName { get; private set; }
    public string ClaimValue { get; private set; }
    public string Icon { get; private set; }
    public string AccessUrl { get; private set; }
    public string RoleName { get; private set; }
    public string ControlType { get; private set; }
    public string ReportId { get; private set; }
    public bool IsDefaultReport { get; private set; }

    private UserClaim()
    {

    }
}
