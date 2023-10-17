using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Chroma;

public class ChromaQueryResultModel
{
    [JsonPropertyName("ids")]
    public List<List<string>> Ids { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public List<List<float[]>> Embeddings { get; set; } = new();

    [JsonPropertyName("metadatas")]
    public List<List<Dictionary<string, object>>> Metadatas { get; set; } = new();

    [JsonPropertyName("distances")]
    public List<List<double>> Distances { get; set; } = new();
}
