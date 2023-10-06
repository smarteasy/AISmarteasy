using System.Text;
using System.Text.Json.Serialization;
using SemanticKernel.Web;

namespace SemanticKernel.Memory.Pinecone;

internal sealed class DeleteRequest
{
    [JsonPropertyName("ids")]
    public IEnumerable<string>? Ids { get; set; }

    [JsonPropertyName("deleteAll")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DeleteAll { get; set; }

    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }

    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Filter { get; set; }

    public static DeleteRequest GetDeleteAllVectorsRequest()
    {
        return new DeleteRequest(true);
    }

    public static DeleteRequest ClearNamespace(string indexNamespace)
    {
        return new DeleteRequest(true)
        {
            Namespace = indexNamespace
        };
    }

    public static DeleteRequest DeleteVectors(IEnumerable<string>? ids)
    {
        return new DeleteRequest(ids);
    }

    public DeleteRequest FilterBy(Dictionary<string, object>? filter)
    {
        Filter = filter;
        return this;
    }

    public DeleteRequest FromNamespace(string? indexNamespace)
    {
        Namespace = indexNamespace;
        return this;
    }

    public DeleteRequest Clear(bool deleteAll)
    {
        DeleteAll = deleteAll;
        return this;
    }

    public HttpRequestMessage Build()
    {
        if (Filter != null)
        {
            Filter = PineconeUtils.ConvertFilterToPineconeFilter(Filter);
        }

        HttpRequestMessage? request = HttpRequest.CreatePostRequest(
            "/vectors/delete",
            this);

        request.Headers.Add("accept", "application/json");

        return request;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("DeleteRequest: ");

        if (Ids != null)
        {
            sb.Append($"Deleting {Ids.Count()} vectors, {string.Join(", ", Ids)},");
        }

        if (DeleteAll != null)
        {
            sb.Append("Deleting All vectors,");
        }

        if (Namespace != null)
        {
            sb.Append($"From Namespace: {Namespace}, ");
        }

        if (Filter == null)
        {
            return sb.ToString();
        }

        sb.Append("With Filter: ");

        foreach (var pair in Filter)
        {
            sb.Append($"{pair.Key}={pair.Value}, ");
        }

        return sb.ToString();
    }

    private DeleteRequest(IEnumerable<string>? ids)
    {
        Ids = ids ?? new List<string>();
    }

    private DeleteRequest(bool clear)
    {
        Ids = new List<string>();
        DeleteAll = clear;
    }
}
