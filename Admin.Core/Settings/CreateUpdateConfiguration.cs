using IDP.Domain.AggregateRoots.Configurations;
using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Configurations;

public class CreateUpdateConfiguration
{
    public int Id { get; set; }
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
    public ValueTypes ValueType { get; set; }
    public ConfigurationScopes Scope { get; set; }
    public bool IsEditable { get; set; }
}
