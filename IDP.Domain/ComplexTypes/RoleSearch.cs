namespace IDP.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public partial class RoleSearch
{
    public int Id { get; private set; }
    public string TenantName { get; private set; }
    public string RoleName { get; private set; }
    public string Active { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private RoleSearch()
    {

    }
}

public partial class RoleSearch
{
    public string UpdatedBy
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}
