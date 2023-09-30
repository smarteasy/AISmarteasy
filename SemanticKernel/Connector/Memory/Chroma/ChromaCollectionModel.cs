using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Chroma;

public class ChromaCollectionModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
