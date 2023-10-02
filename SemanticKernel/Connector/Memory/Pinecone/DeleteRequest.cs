using System.Text;
using System.Text.Json.Serialization;
using SemanticKernel.Web;

namespace SemanticKernel.Connector.Memory.Pinecone;

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
        this.Filter = filter;
        return this;
    }

    public DeleteRequest FromNamespace(string? indexNamespace)
    {
        this.Namespace = indexNamespace;
        return this;
    }

    public DeleteRequest Clear(bool deleteAll)
    {
        this.DeleteAll = deleteAll;
        return this;
    }

    public HttpRequestMessage Build()
    {
        if (this.Filter != null)
        {
            this.Filter = PineconeUtils.ConvertFilterToPineconeFilter(this.Filter);
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

        if (this.Ids != null)
        {
            sb.Append($"Deleting {this.Ids.Count()} vectors, {string.Join(", ", this.Ids)},");
        }

        if (this.DeleteAll != null)
        {
            sb.Append("Deleting All vectors,");
        }

        if (this.Namespace != null)
        {
            sb.Append($"From Namespace: {this.Namespace}, ");
        }

        if (this.Filter == null)
        {
            return sb.ToString();
        }

        sb.Append("With Filter: ");

        foreach (var pair in this.Filter)
        {
            sb.Append($"{pair.Key}={pair.Value}, ");
        }

        return sb.ToString();
    }

    private DeleteRequest(IEnumerable<string>? ids)
    {
        this.Ids = ids ?? new List<string>();
    }

    private DeleteRequest(bool clear)
    {
        this.Ids = new List<string>();
        this.DeleteAll = clear;
    }
}
