using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

public class IndexStats
{
    public IndexStats(
        Dictionary<string, IndexNamespaceStats> namespaces,
        int dimension = default,
        float indexFullness = default,
        long totalVectorCount = default)
    {
        this.Namespaces = namespaces;
        this.Dimension = dimension;
        this.IndexFullness = indexFullness;
        this.TotalVectorCount = totalVectorCount;
    }

    [JsonPropertyName("namespaces")]
    public Dictionary<string, IndexNamespaceStats> Namespaces { get; set; }

    [JsonPropertyName("dimension")]
    public int Dimension { get; set; }

    [JsonPropertyName("indexFullness")]
    public float IndexFullness { get; set; }

    [JsonPropertyName("totalVectorCount")]
    public long TotalVectorCount { get; set; }
}
