using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Memory.Chroma;

internal sealed class GetEmbeddingsRequest
{
    [JsonIgnore]
    public string CollectionId { get; set; }

    [JsonPropertyName("ids")]
    public string[] Ids { get; set; }

    [JsonPropertyName("include")]
    public string[]? Include { get; set; }

    public static GetEmbeddingsRequest Create(string collectionId, string[] ids, string[]? include = null)
    {
        return new GetEmbeddingsRequest(collectionId, ids, include);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest($"collections/{this.CollectionId}/get", this);
    }

    private GetEmbeddingsRequest(string collectionId, string[] ids, string[]? include = null)
    {
        CollectionId = collectionId;
        Ids = ids;
        Include = include;
    }
}
