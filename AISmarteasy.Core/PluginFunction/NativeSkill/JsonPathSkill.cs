using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class JsonPathSkill
{
    public static class Parameters
    {
        public const string JsonPath = "jsonpath";
    }

    [SKFunction, Description("Retrieve the value of a JSON element from a JSON string using a JsonPath query.")]
    public string GetJsonElementValue(
        [Description("JSON string")] string json,
        [Description("JSON path query.")] string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Variable was null or whitespace", nameof(json));
        }

        JObject jsonObject = JObject.Parse(json);

        JToken? token = jsonObject.SelectToken(jsonPath);

        return token?.Value<string>() ?? string.Empty;
    }

    [SKFunction, Description("Retrieve a collection of JSON elements from a JSON string using a JsonPath query.")]
    public string GetJsonElements(
        [Description("JSON string")] string json,
        [Description("JSON path query.")] string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Variable was null or whitespace", nameof(json));
        }

        JObject jsonObject = JObject.Parse(json);

        JToken[] tokens = jsonObject.SelectTokens(jsonPath).ToArray();

        return JsonConvert.SerializeObject(tokens, Formatting.None);
    }
}
