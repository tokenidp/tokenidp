using IDP.Domain.AggregateRoots;

namespace Admin.Core.Configurations;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and used for automapper")]
internal class ConfigurationDto
{
    internal static Expression<Func<Configuration, ConfigurationDto>> Projection =>
        t => new ConfigurationDto
        {
            Id = t.Id,
            ConfigKey = t.ConfigKey,
            ConfigValue = t.ConfigValue,
            TenantId = t.TenantId
        };

    public int Id { get; set; }
    public int TenantId { get; private set; }
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
    public bool? IsDisplay { get; private set; }
}
