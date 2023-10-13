using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Chroma;

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
