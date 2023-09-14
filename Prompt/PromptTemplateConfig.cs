﻿using System.Text.Json.Serialization;

namespace SemanticKernel.Prompt;

public class PromptTemplateConfig
{
    public class CompletionConfig
    {
        [JsonPropertyName("temperature")]
        [JsonPropertyOrder(1)]
        public double Temperature { get; set; } = 0.0f;

        [JsonPropertyName("top_p")]
        [JsonPropertyOrder(2)]
        public double TopP { get; set; } = 0.0f;

        [JsonPropertyName("presence_penalty")]
        [JsonPropertyOrder(3)]
        public double PresencePenalty { get; set; } = 0.0f;

        [JsonPropertyName("frequency_penalty")]
        [JsonPropertyOrder(4)]
        public double FrequencyPenalty { get; set; } = 0.0f;

        [JsonPropertyName("max_tokens")]
        [JsonPropertyOrder(5)]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("stop_sequences")]
        [JsonPropertyOrder(6)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> StopSequences { get; set; } = new();

        [JsonPropertyName("chat_system_prompt")]
        [JsonPropertyOrder(7)]
        public string? ChatSystemPrompt { get; set; }

        [JsonPropertyName("service_id")]
        [JsonPropertyOrder(8)]
        public string? ServiceId { get; set; }
    }

    public class InputParameter
    {
        [JsonPropertyName("name")]
        [JsonPropertyOrder(1)]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [JsonPropertyOrder(2)]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("defaultValue")]
        [JsonPropertyOrder(3)]
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class InputConfig
    {
        [JsonPropertyName("parameters")]
        [JsonPropertyOrder(1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<InputParameter> Parameters { get; set; } = new();
    }

    [JsonPropertyName("schema")]
    [JsonPropertyOrder(1)]
    public int Schema { get; set; } = 1;

    [JsonPropertyName("type")]
    [JsonPropertyOrder(2)]
    public string Type { get; set; } = "completion";

    [JsonPropertyName("description")]
    [JsonPropertyOrder(3)]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("completion")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionConfig Completion { get; set; } = new();

    [JsonPropertyName("default_services")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> DefaultServices { get; set; } = new();

    [JsonPropertyName("input")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InputConfig Input { get; set; } = new();

    public PromptTemplateConfig Compact()
    {
        if (this.Completion.StopSequences.Count == 0)
        {
            this.Completion.StopSequences = null!;
        }

        if (this.DefaultServices.Count == 0)
        {
            this.DefaultServices = null!;
        }

        return this;
    }

    public static PromptTemplateConfig FromJson(string json)
    {
        var result = Json.Deserialize<PromptTemplateConfig>(json);
        return result ?? throw new ArgumentException("Unable to deserialize prompt template config from argument. The deserialization returned null.", nameof(json));
    }
}
