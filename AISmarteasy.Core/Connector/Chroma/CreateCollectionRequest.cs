using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Chroma;


internal sealed class CreateCollectionRequest
{
    [JsonPropertyName("name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("get_or_create")]
    public bool GetOrCreate => true;

    public static CreateCollectionRequest Create(string collectionName)
    {
        return new CreateCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest("collections", this);
    }

    private CreateCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
}
