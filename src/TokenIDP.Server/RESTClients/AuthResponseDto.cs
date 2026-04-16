using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenIDP.Server.RESTClients;

public class AuthResponseDto
{
    public bool IsSuccess { get; set; } = default!;
    public string Error { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public string AuthorizationCode { get; set; } = default!;
    public int? UserId { get; set; } = default!;
    public bool? TwoFactorEnabled { get; set; } = default!;

    private AuthResponseDto(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    [JsonConstructor] // Tells the library: "Use THIS one"
    public AuthResponseDto(bool isSuccess, string error, string correlationId, string authorizationCode, int? userId, bool? twoFactorEnabled)
    {
        IsSuccess = isSuccess;
        Error = error;
        CorrelationId = correlationId;
        AuthorizationCode = authorizationCode;
        UserId = userId;
        TwoFactorEnabled = twoFactorEnabled;
    }

    public static AuthResponseDto Failure(string error)
    {
        return new AuthResponseDto(false) { Error = error };
    }

    public static AuthResponseDto FromJson(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return Failure("Invalid authentication response.");
        }

        var isSuccess = ReadBoolean(payload, "isSuccess", "IsSuccess");
        var error = ReadString(payload, "error", "Error");
        var correlationId = ReadString(payload, "correlationId", "CorrelationId");
        var authorizationCode = ReadString(payload, "authorizationCode", "AuthorizationCode");
        var userId = ReadNullableInt(payload, "userId", "UserId");
        var twoFactorEnabled = ReadNullableBoolean(payload, "twoFactorEnabled", "TwoFactorEnabled");

        return new AuthResponseDto(
            isSuccess,
            error,
            correlationId,
            authorizationCode,
            userId,
            twoFactorEnabled);
    }

    private static string ReadString(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }

            if (property.ValueKind == JsonValueKind.Null || property.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            if (property.ValueKind == JsonValueKind.Object)
            {
                var nested = ReadNestedString(property, "error", "message", "Error", "Message");
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }

            if (property.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.EnumerateArray())
                {
                    var nested = item.ValueKind == JsonValueKind.String
                        ? item.GetString()
                        : ReadNestedString(item, "error", "message", "Error", "Message");

                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return property.ToString();
        }

        return string.Empty;
    }

    private static string ReadNestedString(JsonElement payload, params string[] propertyNames)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        foreach (var propertyName in propertyNames)
        {
            if (payload.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool ReadBoolean(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.String &&
                bool.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    private static bool? ReadNullableBoolean(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Null || property.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.String &&
                bool.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static int? ReadNullableInt(JsonElement payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!payload.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            {
                return value;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

