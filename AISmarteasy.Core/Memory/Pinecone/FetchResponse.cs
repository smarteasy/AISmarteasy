using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory.Pinecone;

internal sealed class FetchResponse
{
    [JsonConstructor]
    public FetchResponse(Dictionary<string, PineconeDocument> vectors, string nameSpace = "")
    {
        Vectors = vectors;
        Namespace = nameSpace;
    }

    [JsonPropertyName("vectors")]
    public Dictionary<string, PineconeDocument> Vectors { get; set; }

    public IEnumerable<PineconeDocument> WithoutEmbeddings()
    {
        return Vectors.Values.Select(v => PineconeDocument.Create(v.Id).WithMetadata(v.Metadata));
    }

    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }
}
