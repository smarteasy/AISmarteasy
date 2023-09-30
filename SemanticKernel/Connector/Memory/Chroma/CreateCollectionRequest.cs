using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Chroma;


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
        this.CollectionName = collectionName;
    }
}
