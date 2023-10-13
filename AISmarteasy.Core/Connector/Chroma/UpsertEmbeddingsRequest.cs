using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Chroma;

internal sealed class UpsertEmbeddingsRequest
{
    [JsonIgnore]
    public string CollectionId { get; set; }

    [JsonPropertyName("ids")]
    public string[] Ids { get; set; }

    [JsonPropertyName("embeddings")]
    public ReadOnlyMemory<float>[] Embeddings { get; set; }

    [JsonPropertyName("metadatas")]
    public object[]? Metadatas { get; set; }

    public static UpsertEmbeddingsRequest Create(string collectionId, string[] ids, ReadOnlyMemory<float>[] embeddings, object[]? metadatas = null)
    {
        return new UpsertEmbeddingsRequest(collectionId, ids, embeddings, metadatas);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest($"collections/{CollectionId}/upsert", this);
    }

    private UpsertEmbeddingsRequest(string collectionId, string[] ids, ReadOnlyMemory<float>[] embeddings, object[]? metadatas = null)
    {
        CollectionId = collectionId;
        Ids = ids;
        Embeddings = embeddings;
        Metadatas = metadatas;
    }
}
