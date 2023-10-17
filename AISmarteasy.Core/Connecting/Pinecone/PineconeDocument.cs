using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AISmarteasy.Core.Memory;

namespace AISmarteasy.Core.Connecting.Pinecone;

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
    public string? Text => Metadata?.TryGetValue("text", out var text) == true ? text.ToString() : null;

    [JsonIgnore]
    public string? DocumentId => Metadata?.TryGetValue("document_Id", out var docId) == true ? docId.ToString() : null;

    [JsonIgnore]
    public string? SourceId => Metadata?.TryGetValue("source_Id", out var sourceId) == true ? sourceId.ToString() : null;

    [JsonIgnore]
    public string? CreatedAt => Metadata?.TryGetValue("created_at", out var createdAt) == true ? createdAt.ToString() : null;

    [JsonConstructor]
    public PineconeDocument(
        ReadOnlyMemory<float> values = default,
        string? id = default,
        Dictionary<string, object>? metadata = null,
        SparseVectorData? sparseValues = null,
        float? score = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Values = values;
        Metadata = metadata ?? new Dictionary<string, object>();
        SparseValues = sparseValues;
        Score = score;
    }

    public static PineconeDocument Create(string? id = default, ReadOnlyMemory<float> values = default)
    {
        return new PineconeDocument(values, id);
    }

    public PineconeDocument WithSparseValues(SparseVectorData? sparseValues)
    {
        SparseValues = sparseValues;
        return this;
    }

    public PineconeDocument WithMetadata(Dictionary<string, object>? metadata)
    {
        Metadata = metadata;
        return this;
    }

    public string GetSerializedMetadata()
    {
        if (Metadata == null)
        {
            return string.Empty;
        }

        var propertiesToSkip = new HashSet<string>() { "text", "document_Id", "source_Id", "created_at" };

        var distinctMetadata = Metadata
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

    public MemoryRecord ToMemoryRecord(bool transferVectorOwnership)
    {
        ReadOnlyMemory<float> embedding = Values;

        string additionalMetadataJson = GetSerializedMetadata();

        MemoryRecordMetadata memoryRecordMetadata = new(
            false,
            DocumentId ?? string.Empty,
            Text ?? string.Empty,
            string.Empty,
            SourceId ?? string.Empty,
            additionalMetadataJson
        );

        DateTimeOffset? timestamp = CreatedAt != null
            ? DateTimeOffset.Parse(CreatedAt, DateTimeFormatInfo.InvariantInfo)
            : null;

        return MemoryRecord.FromMetadata(memoryRecordMetadata, embedding, Id, timestamp);
    }
}
