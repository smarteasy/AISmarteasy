using System.Text.Json;
using SemanticKernel.Memory;

namespace SemanticKernel.Prompt;

internal static class Json
{
    internal static string Serialize(object? o) => JsonSerializer.Serialize(o, s_options);

    internal static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, s_options);

    internal static string ToJson(this object o) => JsonSerializer.Serialize(o, s_options);

    private static readonly JsonSerializerOptions s_options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            MaxDepth = 20,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        options.Converters.Add(new ReadOnlyMemoryConverter());

        return options;
    }
}
