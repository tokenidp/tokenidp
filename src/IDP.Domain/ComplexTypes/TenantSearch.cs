namespace IDP.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public partial class TenantSearch
{
    public int Id { get; set; }
    public string TenantName { get; private set; }
    public string TenantCode { get; private set; }
    public string Email { get; private set; }
    public string Active { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private TenantSearch()
    {

    }
}

public partial class TenantSearch
{
    public string UpdatedBy
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}
