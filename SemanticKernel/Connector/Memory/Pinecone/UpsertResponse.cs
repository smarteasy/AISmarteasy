using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class UpsertResponse
{
    public UpsertResponse(int upsertedCount = default)
    {
        this.UpsertedCount = upsertedCount;
    }

    [JsonPropertyName("upsertedCount")]
    public int UpsertedCount { get; set; }
}
