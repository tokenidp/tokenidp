namespace IDP.Core.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class RoleSearch
{
    public int Id { get; private set; }
    public string TenantName { get; private set; }
    public string RoleName { get; private set; }
    public string Active { get; private set; }
    public string UpdateBy { get; private set; }

    private RoleSearch()
    {

    }
}
