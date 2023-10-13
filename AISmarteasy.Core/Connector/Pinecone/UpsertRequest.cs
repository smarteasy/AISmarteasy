using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Pinecone;

internal sealed class UpsertRequest
{
    [JsonPropertyName("vectors")]
    public List<PineconeDocument> Vectors { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    public static UpsertRequest UpsertVectors(IEnumerable<PineconeDocument> vectorRecords)
    {
        UpsertRequest request = new();

        request.Vectors.AddRange(vectorRecords);

        return request;
    }

    public UpsertRequest ToNamespace(string? indexNamespace)
    {
        Namespace = indexNamespace;
        return this;
    }

    public HttpRequestMessage Build()
    {
        var request = HttpRequest.CreatePostRequest("/vectors/upsert", this);
        request.Headers.Add("accept", "application/json");
        return request;
    }

    [JsonConstructor]
    private UpsertRequest()
    {
        Vectors = new List<PineconeDocument>();
    }
}
