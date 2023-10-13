using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory;

public class MemoryQueryResult
{
    public MemoryRecordMetadata Metadata { get; }

    public double Relevance { get; }

    [JsonConverter(typeof(ReadOnlyMemoryConverter))]
    public ReadOnlyMemory<float>? Embedding { get; }

    [JsonConstructor]
    public MemoryQueryResult(
        MemoryRecordMetadata metadata,
        double relevance,
        ReadOnlyMemory<float>? embedding)
    {
        Metadata = metadata;
        Relevance = relevance;
        Embedding = embedding;
    }

    internal static MemoryQueryResult FromMemoryRecord(
        MemoryRecord rec,
        double relevance)
    {
        return new MemoryQueryResult(
            (MemoryRecordMetadata)rec.Metadata.Clone(),
            relevance,
            rec.Embedding.IsEmpty ? null : rec.Embedding);
    }
}
