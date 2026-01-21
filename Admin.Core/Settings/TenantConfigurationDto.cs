using IDP.Domain.AggregateRoots;
using IDP.Domain.Specifications;

namespace Admin.Core.Configurations;

internal class TenantConfigurationDto
{
    internal static Expression<Func<Configuration, TenantConfigurationDto>> Projection =>
        c => new TenantConfigurationDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Key = c.ConfigKey,
            Value = c.ConfigValue,
            ValueType = c.ValueType,
            Scope = c.Scope.ToString(),
            IsEditable = c.IsEditable
        };

    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public ValueTypes ValueType { get; set; }
    public string? Scope { get; set; }
    public bool IsEditable { get; set; }
}