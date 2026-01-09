using System.ComponentModel.DataAnnotations;

namespace Admin.Core.Configurations;

internal class CreateUpdateConfiguration
{
    public int Id { get; set; }
    [Required]
    public required string ConfigKey { get; set; }
    [Required]
    public required string ConfigValue { get; set; }
    public bool? IsDisplay { get; set; }
    public bool IsEditable { get; set; }
}
