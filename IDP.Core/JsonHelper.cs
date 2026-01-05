using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDP.Core;

internal class JsonHelper
{
    private readonly JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new PrivateSetterConverterFactory() }
    };

    internal T DeserializeObject<T>(string value, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        return JsonSerializer.Deserialize<T>(value, options);
    }

    internal string SerializeObject(object value, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        return JsonSerializer.Serialize(value, options);
    }

    internal string SerializeFormattedObject(object value)
    {
        var formattedOptions = new JsonSerializerOptions(_defaultOptions)
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(value, formattedOptions);
    }

    internal Dictionary<string, T> DeserializeDynamicObject<T>(string value, JsonSerializerOptions options = null)
    {
        options ??= _defaultOptions;
        var keyValues = new Dictionary<string, T>();
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value, options);

        foreach (var kvp in values)
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(kvp.Value.GetRawText(), options);
            keyValues.Add(kvp.Key, deserializedValue);
        }

        return keyValues;
    }
}

internal class PrivateSetterConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(prop => prop.SetMethod is { IsPrivate: true });
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(PrivateSetterConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

internal class PrivateSetterConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var instance = (T)Activator.CreateInstance(typeToConvert, nonPublic: true);
        using var doc = JsonDocument.ParseValue(ref reader);
        foreach (var prop in typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (doc.RootElement.TryGetProperty(prop.Name, out var jsonProp))
            {
                var value = JsonSerializer.Deserialize(jsonProp.GetRawText(), prop.PropertyType, options);
                prop.SetValue(instance, value);
            }
        }
        return instance;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
