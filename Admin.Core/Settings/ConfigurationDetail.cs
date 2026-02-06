using IDP.Domain.AggregateRoots.Configurations;

namespace Admin.Core.Configurations;

public class ConfigurationDetail
{
    public ConfigurationDetail(int id, 
        int tenantId, 
        string configKey, 
        string configValue, 
        ValueTypes valueType, 
        string? scope, 
        bool isEditable)
    {
        Id = id;
        TenantId = tenantId;
        ConfigKey = configKey;
        ConfigValue = configValue;
        ValueType = valueType;
        Scope = scope;
        IsEditable = isEditable;
    }

    public int Id { get; set; }
    public int TenantId { get; private set; }
    public string ConfigKey { get; set; }
    public string ConfigValue { get; set; }
    public ValueTypes ValueType { get; private set; }
    public string? Scope { get; private set; }
    public bool IsEditable { get; private set; }
}
