using System.Globalization;
using System.Reflection;
using System.Text.Json;
using SneakyLog.Attributes;

namespace SneakyLog.Utilities;

internal class TraceObjectSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string SerializeObject(object? obj)
    {
        if (obj == null) return "null";
        
        var type = obj.GetType();
        if (IsPrimitive(type)) return FormatValue(obj);

        var traceableObj = type.GetCustomAttribute<TraceableObjectAttribute>();
        var strategy = traceableObj?.Strategy ?? TraceStrategy.OptIn;
        
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var tracedProperties = new Dictionary<string, object?>();

        foreach (var prop in properties)
        {
            if (ShouldTraceProperty(prop, strategy))
            {
                var value = prop.GetValue(obj);
                var traceableAttr = prop.GetCustomAttribute<TraceableAttribute>();
                
                if (traceableAttr?.MaskValue == true)
                {
                    value = MaskValue(value);
                }
                
                tracedProperties[prop.Name] = value;
            }
        }

        if (!tracedProperties.Any())
            return "{...}";

        return JsonSerializer.Serialize(tracedProperties, JsonOptions);
    }

    private static bool ShouldTraceProperty(PropertyInfo prop, TraceStrategy strategy)
    {
        var traceable = prop.GetCustomAttribute<TraceableAttribute>();
        var notTraceable = prop.GetCustomAttribute<NotTraceableAttribute>();

        return strategy switch
        {
            TraceStrategy.OptIn => traceable != null,
            TraceStrategy.OptOut => notTraceable == null,
            _ => false
        };
    }

    private static object? MaskValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            string str => MaskString(str),
            IEnumerable<char> chars => MaskString(new string(chars.ToArray())),
            IFormattable => "*****",
            _ => "*****"
        };
    }

    private static string MaskString(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (str.Length <= 4) return new string('*', str.Length);

        return $"{str[..2]}{new string('*', str.Length - 4)}{str[^2..]}";
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            string str => $"\"{str}\"",
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }

    private static bool IsPrimitive(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(Guid) || type.IsEnum;
    }
}