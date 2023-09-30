using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Chroma;

public class ChromaEmbeddingsModel
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public List<float[]> Embeddings { get; set; } = new();

    [JsonPropertyName("metadatas")]
    public List<Dictionary<string, object>> Metadatas { get; set; } = new();
}
