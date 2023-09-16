namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public class CompleteRequestSettings
{
    public double Temperature { get; set; }

    public double TopP { get; set; }

    public double PresencePenalty { get; set; }

    public double FrequencyPenalty { get; set; }

    public int? MaxTokens { get; set; }

    public IList<string> StopSequences { get; set; } = Array.Empty<string>();

    public int ResultsPerPrompt { get; set; } = 1;

    public string ChatSystemPrompt { get; set; } = "Assistant is a large language model.";

    public IDictionary<int, int> TokenSelectionBiases { get; set; } = new Dictionary<int, int>();

    public static CompleteRequestSettings FromCompletionConfig(PromptTemplateConfig.CompletionConfig config)
    {
        var settings = new CompleteRequestSettings
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
