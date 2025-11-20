using System.Linq.Expressions;

namespace IDP.Core.Admin.Configurations;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and used for automapper")]
public class ConfigurationDto
{
    public static Expression<Func<Configuration, ConfigurationDto>> Projection =>
        t => new ConfigurationDto
        {
            Id = t.Id,
            ConfigKey = t.ConfigKey,
            ConfigValue = t.ConfigValue,
            IsDisplay = t.IsDisplay,
            TenantId = t.TenantId
        };

    public int Id { get; set; }
    public int TenantId { get; private set; }
    public string ConfigKey { get; private set; }
    public string ConfigValue { get; private set; }
    public bool? IsDisplay { get; private set; }
}
