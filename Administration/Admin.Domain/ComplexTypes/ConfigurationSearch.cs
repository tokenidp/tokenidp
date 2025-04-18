using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class ConfigurationSearch
{
    public int Id { get; set; }
    public string TenantName { get; private set; }
    public string ConfigKey { get; private set; }
    public string UserName { get; private set; }
    public string ConfigValue { get; private set; }
    public string UpdateBy { get; private set; }

    private ConfigurationSearch()
    {

    }
}
