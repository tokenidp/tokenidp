using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Identity.Infrastructure;

public class JsonHelper
{
    private readonly JsonSerializerSettings _defaultSettings
        = new()
        {
            ContractResolver = new PrivateResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

    public T DeserializeObject<T>(string value,
        JsonSerializerSettings settings = null)
    {
        settings ??= _defaultSettings;
        return JsonConvert.DeserializeObject<T>(value, settings);
    }

    public string SerializeObject(object value,
        JsonSerializerSettings settings = null)
    {
        settings ??= _defaultSettings;
        return JsonConvert.SerializeObject(value, settings);
    }

    public string SerializeFormattedObject(object value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented,
            _defaultSettings);
    }

    public Dictionary<string, T> DeserializeDynamicObject<T>(string value,
        JsonSerializerSettings settings = null)
    {
        settings ??= _defaultSettings;
        Dictionary<string, T> keyValues = new();

        JObject values = JObject.Parse(value);

        foreach (var jItem in values)
        {
            JObject valueObject = (JObject)jItem.Value;
            T result = JsonConvert.DeserializeObject<T>(valueObject.ToString(), settings);
            keyValues.Add(jItem.Key, result);
        }

        return keyValues;
    }

    public static bool IsValidJsonFor<T>(T schemaType, string jsonString)
        where T : class
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return false;
        }

        JSchemaGenerator generator = new();
        generator.ContractResolver = new PrivateResolver();
        JSchema schema = generator.Generate(typeof(T));

        var token = JToken.Parse(jsonString);
        JObject jObject = null;
        if (token is JArray)
        {
            return false;
        }
        else if (token is JObject)
        {
            jObject = JObject.Parse(jsonString);
        }

        IList<string> errorMessages;

        return jObject.IsValid(schema, out errorMessages);
    }
}

public class PrivateResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(
        MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        if (!prop.Writable)
        {
            var property = member as PropertyInfo;
            var hasPrivateSetter = property?.GetSetMethod(true) != null;
            prop.Writable = hasPrivateSetter;
        }
        return prop;
    }
}
