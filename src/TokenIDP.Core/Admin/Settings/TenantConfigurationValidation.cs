using System.Text.Json;
using TokenIDP.Domain.AggregateRoots.Configurations;

namespace TokenIDP.Core.Admin.Configurations;

internal static class TenantConfigurationValidation
{
    internal static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    internal static bool IsSupportedValueType(ValueTypes valueType)
    {
        return valueType == ValueTypes.String
            || valueType == ValueTypes.Int
            || valueType == ValueTypes.Bool
            || valueType == ValueTypes.Json;
    }

    internal static Result ValidateValue(ValueTypes valueType, string value)
    {
        if (!IsSupportedValueType(valueType))
        {
            return Result.Failure("configuration.value_type.invalid",
                "Configuration value type is not supported.");
        }

        switch (valueType)
        {
            case ValueTypes.Bool:
                return bool.TryParse(value, out _)
                    ? Result.Success(0)
                    : Result.Failure("configuration.value.invalid",
                        "Configuration value must be a boolean.");

            case ValueTypes.Int:
                return int.TryParse(value, out _)
                    ? Result.Success(0)
                    : Result.Failure("configuration.value.invalid",
                        "Configuration value must be an integer.");

            case ValueTypes.Json:
                try
                {
                    using var _ = JsonDocument.Parse(value);
                    return Result.Success(0);
                }
                catch (JsonException)
                {
                    return Result.Failure("configuration.value.invalid",
                        "Configuration value must be valid JSON.");
                }

            default:
                return Result.Success(0);
        }
    }
}

