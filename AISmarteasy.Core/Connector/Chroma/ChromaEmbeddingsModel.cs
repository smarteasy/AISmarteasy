using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connector.Chroma;

public class ChromaEmbeddingsModel
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public List<float[]> Embeddings { get; set; } = new();

    [JsonPropertyName("metadatas")]
    public List<Dictionary<string, object>> Metadatas { get; set; } = new();
}
