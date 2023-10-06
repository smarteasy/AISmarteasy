using System.Text.Json.Serialization;

namespace SemanticKernel.Memory;

public class MemoryRecordMetadata : ICloneable
{
    [JsonInclude]
    [JsonPropertyName("is_reference")]
    public bool IsReference { get; }

    [JsonInclude]
    [JsonPropertyName("external_source_name")]
    public string ExternalSourceName { get; }

    [JsonInclude]
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonInclude]
    [JsonPropertyName("description")]
    public string Description { get; }

    [JsonInclude]
    [JsonPropertyName("text")]
    public string Text { get; }

    [JsonInclude]
    [JsonPropertyName("additional_metadata")]
    public string AdditionalMetadata { get; }

    [JsonConstructor]
    public MemoryRecordMetadata(
        bool isReference,
        string id,
        string text,
        string description,
        string externalSourceName,
        string additionalMetadata
    )
    {
        IsReference = isReference;
        ExternalSourceName = externalSourceName;
        Id = id;
        Text = text;
        Description = description;
        AdditionalMetadata = additionalMetadata;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}
