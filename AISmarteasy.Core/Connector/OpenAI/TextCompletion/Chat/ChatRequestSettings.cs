namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;

public class ChatRequestSettings
{
    public double Temperature { get; set; }

    public double TopP { get; set; }

    public double PresencePenalty { get; set; }

    public double FrequencyPenalty { get; set; }

    public IList<string> StopSequences { get; set; } = Array.Empty<string>();

    public int ResultsPerPrompt { get; set; } = 1;

    public int? MaxTokens { get; set; }

    public IDictionary<int, int> TokenSelectionBiases { get; set; } = new Dictionary<int, int>();

    public static ChatRequestSettings FromCompletionConfig(PromptTemplateConfig.CompletionConfig config)
    {
        return new ChatRequestSettings
        {
            Temperature = config.Temperature,
            TopP = config.TopP,
            PresencePenalty = config.PresencePenalty,
            FrequencyPenalty = config.FrequencyPenalty,
            MaxTokens = config.MaxTokens,
            StopSequences = config.StopSequences,
        };
    }
}
