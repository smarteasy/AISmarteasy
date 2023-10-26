using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Planning;

public class SystemStep
{
    [JsonPropertyName("thought")]
    public string Thought { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("action_variables")]
    public Dictionary<string, string> ActionVariables { get; set; } = new();

    [JsonPropertyName("observation")]
    public string Observation { get; set; } = string.Empty;

    [JsonPropertyName("final_answer")]
    public string FinalAnswer { get; set; } = string.Empty;

    [JsonPropertyName("original_response")]
    public string OriginalResponse { get; set; } = string.Empty;
}
