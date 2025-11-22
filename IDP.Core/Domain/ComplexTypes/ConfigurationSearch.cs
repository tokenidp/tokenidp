namespace IDP.Core.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public partial class ConfigurationSearch
{
    public int Id { get; set; }
    public string TenantName { get; private set; }
    public string ConfigKey { get; private set; }
    public string UserName { get; private set; }
    public string ConfigValue { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private ConfigurationSearch()
    {

    }
}

public partial class ConfigurationSearch
{
    public string UpdatedBy
    {
        get
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }
    }
}