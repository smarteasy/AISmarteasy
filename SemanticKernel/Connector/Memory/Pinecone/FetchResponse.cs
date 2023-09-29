using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class FetchResponse
{
    [JsonConstructor]
    public FetchResponse(Dictionary<string, PineconeDocument> vectors, string nameSpace = "")
    {
        this.Vectors = vectors;
        this.Namespace = nameSpace;
    }

    [JsonPropertyName("vectors")]
    public Dictionary<string, PineconeDocument> Vectors { get; set; }

    public IEnumerable<PineconeDocument> WithoutEmbeddings()
    {
        return this.Vectors.Values.Select(v => PineconeDocument.Create(v.Id).WithMetadata(v.Metadata));
    }

    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }
}
