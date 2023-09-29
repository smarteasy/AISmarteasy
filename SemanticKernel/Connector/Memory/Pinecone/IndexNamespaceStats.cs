using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

public class IndexNamespaceStats
{
    public IndexNamespaceStats(long vectorCount = default)
    {
        VectorCount = vectorCount;
    }

    [JsonPropertyName("vectorCount")]
    public long VectorCount { get; }
}
