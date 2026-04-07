namespace TokenIDP.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public partial class RoleSearch
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public string Active { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

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

