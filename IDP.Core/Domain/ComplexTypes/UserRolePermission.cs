namespace IDP.Core.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
internal class UserRolePermission
{
    public int Id { get; private set; }
    public int? ParentId { get; private set; }
    public int UserId { get; private set; }
    public int Sequence { get; private set; }
    public string PermissionType { get; private set; }
    public string PermissionName { get; private set; }
    public string PermissionValue { get; private set; }
    public string Icon { get; private set; }
    public string AccessUrl { get; private set; }
    public string RoleName { get; private set; }
    public string ControlType { get; private set; }

    private UserRolePermission()
    {

    }
}
