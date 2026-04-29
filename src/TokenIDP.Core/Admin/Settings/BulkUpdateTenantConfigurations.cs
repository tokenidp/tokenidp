using System.ComponentModel.DataAnnotations;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

internal class BulkUpdateTenantConfigurations
{
    [Required]
    public List<BulkTenantConfigurationItem> Items { get; set; } = new();
}

internal class BulkTenantConfigurationItem
{
    public int? Id { get; set; }
    [Required]
    public required string Key { get; set; }
    [Required]
    public required string Value { get; set; }
    public ValueTypes ValueType { get; set; }
    public ConfigurationScopes Scope { get; set; }
    public bool IsEditable { get; set; }
}

internal class BulkUpdateTenantConfigurationsResult
{
    public int Requested { get; set; }
    public int Updated { get; set; }
    public int Created { get; set; }
    public List<string> Errors { get; set; } = new();
}

