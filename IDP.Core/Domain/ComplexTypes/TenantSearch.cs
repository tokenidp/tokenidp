namespace IDP.Core.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class TenantSearch
{
    public int Id { get; set; }
    public string TenantName { get; private set; }
    public string TenantCode { get; private set; }
    public string Email { get; private set; }
    public string Active { get; private set; }
    public string UpdateBy { get; private set; }

    private TenantSearch()
    {

    }
}
