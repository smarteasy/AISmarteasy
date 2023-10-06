using System.Text.Json.Serialization;

namespace SemanticKernel.Memory.Pinecone;

public class IndexNamespaceStats
{
    public IndexNamespaceStats(long vectorCount = default)
    {
        VectorCount = vectorCount;
    }

    [JsonPropertyName("vectorCount")]
    public long VectorCount { get; }
}
