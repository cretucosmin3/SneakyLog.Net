using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

public class LoggedPropertyConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return true;  // We'll handle any object type
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Deserialization is not supported");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<LoggedPropertyAttribute>() != null);

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<LoggedPropertyAttribute>();
            var propertyName = string.IsNullOrEmpty(attr.Name) ? prop.Name : attr.Name;
            var propertyValue = prop.GetValue(value);

            writer.WritePropertyName(propertyName);

            if (propertyValue == null)
            {
                writer.WriteNullValue();
            }
            else if (attr.MaskValue)
            {
                writer.WriteStringValue(attr.MaskPattern);
            }
            else if (propertyValue.GetType().IsPrimitive || propertyValue is string)
            {
                writer.WriteStringValue(propertyValue.ToString());
                // JsonSerializer.Serialize(writer, propertyValue, options);
            }
            else
            {
                // Recursively handle nested objects
                JsonSerializer.Serialize(writer, propertyValue, propertyValue.GetType(), options);
            }
        }

        writer.WriteEndObject();
    }
}