namespace TokenIDP.Domain.AggregateRoots.Configurations;

public class ConfigurationShortInfo
{
    public string ConfigKey { get; private set; } = string.Empty;
    public string ConfigValue { get; private set; } = string.Empty;
    public ValueTypes ValueType { get; private set; }

    public ConfigurationShortInfo(string configKey,
        string configValue,
        ValueTypes valueType)
    {
        ConfigKey = configKey;
        ConfigValue = configValue;
        ValueType = valueType;
    }
}

