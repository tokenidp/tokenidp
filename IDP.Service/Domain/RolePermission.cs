
namespace IDP.Service.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class RolePermission : IdentityRoleClaim<int>
{
    public int TenantPermissionId { get; private set; }
    public virtual Role Role { get; private set; }
}
