using IDP.Domain.AggregateRoots;
using IDP.Domain.Specifications;

namespace Admin.Core.Configurations;

internal class ConfigurationDto
{
    internal static Expression<Func<Configuration, ConfigurationDto>> Projection =>
        t => new ConfigurationDto
        {
            Id = t.Id,
            ConfigKey = t.ConfigKey,
            ConfigValue = t.ConfigValue,
            TenantId = t.TenantId,
            ValueType = t.ValueType,
            Scope = t.Scope.ToString(),
            IsEditable = t.IsEditable
        };

    public int Id { get; set; }
    public int TenantId { get; private set; }
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
    public ValueTypes ValueType { get; private set; }
    public string? Scope { get; private set; }
    public bool IsEditable { get; private set; }
}
