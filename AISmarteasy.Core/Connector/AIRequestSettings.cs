using System.Text.Json;
using System.Text.Json.Serialization;
using AISmarteasy.Core.Connector.OpenAI;

namespace AISmarteasy.Core.Connector;

public class AIRequestSettings
{
    public const string FUNCTION_CALL_AUTO = "auto";

    public const string FUNCTION_CALL_NONE = "none";

    [JsonPropertyName("service_id")]
    public string? ServiceId { get; set; } = null;

    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double TopP { get; set; }

    [JsonPropertyName("presence_penalty")]
    public double PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public double FrequencyPenalty { get; set; }

    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; } = DefaultMaxTokens;

    [JsonPropertyName("stop_sequences")]
    public IList<string> StopSequences { get; set; } = Array.Empty<string>();

    [JsonPropertyName("results_per_prompt")]
    public int ResultsPerPrompt { get; set; } = 1;

    [JsonPropertyName("chat_system_prompt")]
    public string ChatSystemPrompt
    {
        get => _chatSystemPrompt;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = DefaultChatSystemPrompt;
            }
            _chatSystemPrompt = value;
        }
    }

    [JsonPropertyName("token_selection_biases")]
    public IDictionary<int, int> TokenSelectionBiases { get; set; } = new Dictionary<int, int>();

    public string? FunctionCall { get; set; }

    public IList<OpenAIFunction>? Functions { get; set; }

    internal static string DefaultChatSystemPrompt { get; } = "Assistant is a large language model.";


    internal static int DefaultMaxTokens { get; } = 256;

    private string _chatSystemPrompt = DefaultChatSystemPrompt;

    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            MaxDepth = 20,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new OpenAIRequestSettingsConverter() }
        };

        return options;
    }

    public static AIRequestSettings FromCompletionConfig(PromptTemplateConfig.CompletionConfig config)
    {
        var settings = new AIRequestSettings
        {
            Temperature = config.Temperature,
            TopP = config.TopP,
            PresencePenalty = config.PresencePenalty,
            FrequencyPenalty = config.FrequencyPenalty,
            MaxTokens = config.MaxTokens,
            StopSequences = config.StopSequences,
        };

        if (!string.IsNullOrWhiteSpace(config.ChatSystemPrompt))
        {
            settings.ChatSystemPrompt = config.ChatSystemPrompt!;
        }

        return settings;
    }
}
