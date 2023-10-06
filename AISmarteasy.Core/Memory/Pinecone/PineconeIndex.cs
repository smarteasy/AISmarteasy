using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory.Pinecone;

public sealed class PineconeIndex
{
    [JsonConstructor]
    public PineconeIndex(IndexDefinition configuration, IndexStatus status)
    {
        Configuration = configuration;
        Status = status;
    }

    [JsonPropertyName("database")]
    public IndexDefinition Configuration { get; set; }

    [JsonPropertyName("status")]
    public IndexStatus Status { get; set; }
}
