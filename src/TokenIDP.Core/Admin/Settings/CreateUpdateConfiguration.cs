using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

public class CreateUpdateConfiguration
{
    public int Id { get; set; }
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
    public ValueTypes ValueType { get; set; }
    public ConfigurationScopes Scope { get; set; }
    public bool IsEditable { get; set; }
}

