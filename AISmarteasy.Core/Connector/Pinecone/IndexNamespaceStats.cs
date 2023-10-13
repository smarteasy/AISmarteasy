using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connector.Pinecone;

public class IndexNamespaceStats
{
    public IndexNamespaceStats(long vectorCount = default)
    {
        VectorCount = vectorCount;
    }

    [JsonPropertyName("vectorCount")]
    public long VectorCount { get; }
}
