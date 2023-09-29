using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

public class PineconeDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("values")]
    [JsonConverter(typeof(ReadOnlyMemoryConverter))]
    public ReadOnlyMemory<float> Values { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("sparseValues")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SparseVectorData? SparseValues { get; set; }

    [JsonPropertyName("score")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? Score { get; set; }

    [JsonIgnore]
    public string? Text => this.Metadata?.TryGetValue("text", out var text) == true ? text.ToString() : null;

    [JsonIgnore]
    public string? DocumentId => this.Metadata?.TryGetValue("document_Id", out var docId) == true ? docId.ToString() : null;

    [JsonIgnore]
    public string? SourceId => this.Metadata?.TryGetValue("source_Id", out var sourceId) == true ? sourceId.ToString() : null;

    [JsonIgnore]
    public string? CreatedAt => this.Metadata?.TryGetValue("created_at", out var createdAt) == true ? createdAt.ToString() : null;

    [JsonConstructor]
    public PineconeDocument(
        ReadOnlyMemory<float> values = default,
        string? id = default,
        Dictionary<string, object>? metadata = null,
        SparseVectorData? sparseValues = null,
        float? score = null)
    {
        this.Id = id ?? Guid.NewGuid().ToString();
        this.Values = values;
        this.Metadata = metadata ?? new Dictionary<string, object>();
        this.SparseValues = sparseValues;
        this.Score = score;
    }

    public static PineconeDocument Create(string? id = default, ReadOnlyMemory<float> values = default)
    {
        return new PineconeDocument(values, id);
    }

     public PineconeDocument WithSparseValues(SparseVectorData? sparseValues)
    {
        this.SparseValues = sparseValues;
        return this;
    }

    public PineconeDocument WithMetadata(Dictionary<string, object>? metadata)
    {
        this.Metadata = metadata;
        return this;
    }

    public string GetSerializedMetadata()
    {
        if (this.Metadata == null)
        {
            return string.Empty;
        }

        var propertiesToSkip = new HashSet<string>() { "text", "document_Id", "source_Id", "created_at" };

        var distinctMetadata = this.Metadata
            .Where(x => !propertiesToSkip.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        return JsonSerializer.Serialize(distinctMetadata, JsonSerializerOptions);
    }

    internal UpdateVectorRequest ToUpdateRequest()
    {
        return UpdateVectorRequest.FromPineconeDocument(this);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var jso = new JsonSerializerOptions();
        jso.Converters.Add(new ReadOnlyMemoryConverter());
        return jso;
    }
}
