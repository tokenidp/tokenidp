namespace Identity.Application.Configurations.Queries;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and used for automapper")]
public class ConfigurationDto : IMapFrom<AppConfiguration>
{
    public int Id { get; set; }
    public int TenantId { get; private set; }
    public string ConfigKey { get; private set; }
    public string ConfigValue { get; private set; }
    public bool? IsDisplay { get; private set; }
    public bool IsDefaultForTenant { get; private set; }
}
