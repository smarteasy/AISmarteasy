using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticKernel.Exception;

namespace SemanticKernel.Memory;

public class MemoryRecord : DataEntryBase
{
    [JsonPropertyName("embedding")]
    [JsonConverter(typeof(ReadOnlyMemoryConverter))]
    public ReadOnlyMemory<float> Embedding { get; }

    [JsonPropertyName("metadata")]
    public MemoryRecordMetadata Metadata { get; }

    [JsonConstructor]
    public MemoryRecord(
        MemoryRecordMetadata metadata,
        ReadOnlyMemory<float> embedding,
        string? key,
        DateTimeOffset? timestamp = null) : base(key, timestamp)
    {
        this.Metadata = metadata;
        this.Embedding = embedding;
    }

    public static MemoryRecord ReferenceRecord(
        string externalId,
        string sourceName,
        string? description,
        ReadOnlyMemory<float> embedding,
        string? additionalMetadata = null,
        string? key = null,
        DateTimeOffset? timestamp = null)
    {
        return new MemoryRecord(
            new MemoryRecordMetadata
            (
                isReference: true,
                externalSourceName: sourceName,
                id: externalId,
                description: description ?? string.Empty,
                text: string.Empty,
                additionalMetadata: additionalMetadata ?? string.Empty
            ),
            embedding,
            key,
            timestamp
        );
    }

    public static MemoryRecord LocalRecord(
        string id,
        string text,
        string? description,
        ReadOnlyMemory<float> embedding,
        string? additionalMetadata = null,
        string? key = null,
        DateTimeOffset? timestamp = null)
    {
        return new MemoryRecord
        (
            new MemoryRecordMetadata
            (
                isReference: false,
                id: id,
                text: text,
                description: description ?? string.Empty,
                externalSourceName: string.Empty,
                additionalMetadata: additionalMetadata ?? string.Empty
            ),
            embedding,
            key,
            timestamp
        );
    }

    public static MemoryRecord FromJsonMetadata(
        string json,
        ReadOnlyMemory<float> embedding,
        string? key = null,
        DateTimeOffset? timestamp = null)
    {
        var metadata = JsonSerializer.Deserialize<MemoryRecordMetadata>(json);
        return metadata != null
            ? new MemoryRecord(metadata, embedding, key, timestamp)
            : throw new SKException("Unable to create memory record from serialized metadata");
    }

    public static MemoryRecord FromMetadata(
        MemoryRecordMetadata metadata,
        ReadOnlyMemory<float> embedding,
        string? key = null,
        DateTimeOffset? timestamp = null)
    {
        return new MemoryRecord(metadata, embedding, key, timestamp);
    }

    public string GetSerializedMetadata()
    {
        return JsonSerializer.Serialize(this.Metadata);
    }
}
