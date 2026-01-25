using IDP.Domain.Specifications;

namespace IDP.Domain.AggregateRoots;

public class Configuration : AuditableAggregate<int>, ITenant
{
    public string ConfigKey { get; private set; } = string.Empty;
    public string ConfigValue { get; private set; } = string.Empty;
    public int TenantId { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsEditable { get; private set; }
    public ValueTypes ValueType { get; private set; }
    public ConfigurationScopes Scope { get; private set; }

    public virtual Tenant Tenant { get; private set; } = default!;

    private Configuration() { }

    private Configuration(int tenantId,
        string configKey,
        string configValue,
        ValueTypes valueType,
        ConfigurationScopes scope,
        bool isEditable)
    {
        TenantId = tenantId;
        ConfigKey = configKey;
        ConfigValue = configValue;
        ValueType = valueType;
        Scope = scope;
        IsEditable = isEditable;
        IsDeleted = false;
    }

    public static Result Create(int tenantId,
        string configKey,
        string configValue,
        ValueTypes valueType,
        ConfigurationScopes scope,
        bool isEditable,
        out Configuration? configuration)
    {
        configuration = null;

        var validation = ValidateInput(configKey, configValue);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        configuration = new Configuration(
            tenantId,
            configKey.Trim(),
            configValue.Trim(),
            valueType,
            scope,
            isEditable);

        return Result.Success(0);
    }

    public Result UpdateConfiguration(
        string configValue,
        ValueTypes valueType,
        ConfigurationScopes scope,
        bool isEditable)
    {
        var validation = ValidateInput(ConfigKey, configValue);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        ConfigValue = configValue.Trim();
        ValueType = valueType;
        Scope = scope;
        IsEditable = isEditable;

        return Result.Success(Id);
    }

    public Result SoftDelete()
    {
        IsDeleted = true;
        return Result.Success(Id);
    }

    private static Result ValidateInput(string configKey, string configValue)
    {
        var validation = ValidateRequired(configKey, "configuration.key.invalid",
            "Configuration key cannot be empty.");

        if (string.IsNullOrWhiteSpace(configValue))
        {
            validation = validation.Combine(Result.Failure(
                "configuration.value.invalid", "Configuration value cannot be empty."));
        }

        return validation;
    }

    private static Result ValidateRequired(string? value, string code, string message)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Result.Failure(code, message)
            : Result.Success(0);
    }
}