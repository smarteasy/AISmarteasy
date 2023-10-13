using System.Text.Json.Serialization;
using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Pinecone;

internal sealed class FetchRequest
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; }

    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }

    public static FetchRequest FetchVectors(IEnumerable<string> ids)
    {
        return new FetchRequest(ids);
    }

    public FetchRequest FromNamespace(string indexNamespace)
    {
        Namespace = indexNamespace;
        return this;
    }

    public HttpRequestMessage Build()
    {
        string path = "/vectors/fetch?";
        string ids = string.Join("&", Ids.Select(id => "ids=" + id));

        path += ids;

        if (!string.IsNullOrEmpty(Namespace))
        {
            path += $"&namespace={Namespace}";
        }

        HttpRequestMessage request = HttpRequest.CreateGetRequest(path);

        request.Headers.Add("accept", "application/json");

        return request;
    }

    private FetchRequest(IEnumerable<string> ids)
    {
        Ids = ids.ToList();
    }
}
