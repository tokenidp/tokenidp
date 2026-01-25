using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Configurations;

internal class CreateUpdateConfiguration
{
    public int Id { get; set; }
    [Required]
    public required string ConfigKey { get; set; }
    [Required]
    public required string ConfigValue { get; set; }
    public ValueTypes ValueType { get; set; }
    public ConfigurationScopes Scope { get; set; }
    public bool IsEditable { get; set; }
}
