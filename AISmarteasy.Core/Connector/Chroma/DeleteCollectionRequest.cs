using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Chroma;

internal sealed class DeleteCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }

    public static DeleteCollectionRequest Create(string collectionName)
    {
        return new DeleteCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"collections/{CollectionName}");
    }

    private DeleteCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
}
