using System.Text.Json;
using Azure.AI.OpenAI;

namespace AISmarteasy.Core.Connector.OpenAI;

public class OpenAIFunctionResponse
{
    public string FunctionName { get; set; } = string.Empty;

    public string PluginName { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();

    public static OpenAIFunctionResponse FromFunctionCall(FunctionCall functionCall)
    {
        OpenAIFunctionResponse response = new();
        if (functionCall.Name.Contains(OpenAIFunction.NAME_SEPARATOR))
        {
            var parts = functionCall.Name.Split(new[] { OpenAIFunction.NAME_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            response.PluginName = parts[0];
            response.FunctionName = parts[1];
        }
        else
        {
            response.FunctionName = functionCall.Name;
        }

        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall.Arguments);
        if (parameters is not null)
        {
            response.Parameters = parameters;
        }

        return response;
    }
}
