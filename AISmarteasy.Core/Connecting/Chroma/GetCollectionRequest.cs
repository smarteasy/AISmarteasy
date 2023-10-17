using System.Text.Json.Serialization;
using AISmarteasy.Core.Handling;

namespace AISmarteasy.Core.Connecting.Chroma;

internal sealed class GetCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }

    public static GetCollectionRequest Create(string collectionName)
    {
        return new GetCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"collections/{CollectionName}");
    }

    private GetCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
}
