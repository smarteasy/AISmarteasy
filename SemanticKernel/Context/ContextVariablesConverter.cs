using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticKernel.Context;

public class ContextVariablesConverter : JsonConverter<ContextVariables>
{
    public override ContextVariables Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var keyValuePairs = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<string, string>>>(ref reader, options);
        var context = new ContextVariables();

        foreach (var kvp in keyValuePairs!)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
               throw new JsonException("'Key' property cannot be null or empty.");
            }

            context.Set(kvp.Key, kvp.Value);
        }

        return context;
    }

    public override void Write(Utf8JsonWriter writer, ContextVariables value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var kvp in value)
        {
            writer.WriteStartObject();
            writer.WriteString("Key", kvp.Key);
            writer.WriteString("Value", kvp.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
