using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

public sealed class PineconeIndex
{
    [JsonConstructor]
    public PineconeIndex(IndexDefinition configuration, IndexStatus status)
    {
        this.Configuration = configuration;
        this.Status = status;
    }

    [JsonPropertyName("database")]
    public IndexDefinition Configuration { get; set; }

    [JsonPropertyName("status")]
    public IndexStatus Status { get; set; }
}
