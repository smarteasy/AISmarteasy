using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Pinecone;

internal sealed class UpdateVectorRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("values")]
    public ReadOnlyMemory<float> Values { get; set; }

    [JsonPropertyName("sparseValues")]
    public SparseVectorData? SparseValues { get; set; }

    [JsonPropertyName("setMetadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    public static UpdateVectorRequest UpdateVector(string id)
    {
        return new UpdateVectorRequest(id);
    }

    public static UpdateVectorRequest FromPineconeDocument(PineconeDocument document)
    {
        return new UpdateVectorRequest(document.Id, document.Values)
        {
            SparseValues = document.SparseValues,
            Metadata = document.Metadata
        };
    }

    public UpdateVectorRequest InNamespace(string? indexNamespace)
    {
        Namespace = indexNamespace;
        return this;
    }

    public UpdateVectorRequest SetMetadata(Dictionary<string, object>? setMetadata)
    {
        Metadata = setMetadata;
        return this;
    }

    public UpdateVectorRequest UpdateSparseValues(SparseVectorData? sparseValues)
    {
        SparseValues = sparseValues;
        return this;
    }

    public UpdateVectorRequest UpdateValues(ReadOnlyMemory<float> values)
    {
        Values = values;
        return this;
    }

    public HttpRequestMessage Build()
    {
        var request = HttpRequest.CreatePostRequest("/vectors/update", this);
        request.Headers.Add("accept", "application/json");
        return request;
    }

    [JsonConstructor]
    private UpdateVectorRequest(string id, ReadOnlyMemory<float> values = default)
    {
        Id = id;
        Values = values;
    }
}
