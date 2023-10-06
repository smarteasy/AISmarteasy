using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Memory.Chroma;

internal sealed class DeleteEmbeddingsRequest
{
    [JsonIgnore]
    public string CollectionId { get; set; }

    [JsonPropertyName("ids")]
    public string[] Ids { get; set; }

    public static DeleteEmbeddingsRequest Create(string collectionId, string[] ids)
    {
        return new DeleteEmbeddingsRequest(collectionId, ids);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest($"collections/{CollectionId}/delete", this);
    }

 
    private DeleteEmbeddingsRequest(string collectionId, string[] ids)
    {
        CollectionId = collectionId;
        Ids = ids;
    }
}
