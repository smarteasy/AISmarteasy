using System.Text.Json.Serialization;
using AISmarteasy.Core.Memory;

namespace AISmarteasy.Core.Connecting.Pinecone;

public sealed class Query
{
    public int TopK { get; set; }

    public string? Namespace { get; set; }

    public Dictionary<string, object>? Filter { get; set; }

    [JsonConverter(typeof(ReadOnlyMemoryConverter))]
    public ReadOnlyMemory<float> Vector { get; set; }

    public string? Id { get; set; }

    public SparseVectorData? SparseVector { get; set; }

    public static Query Create(int topK)
    {
        return new Query()
        {
            TopK = topK
        };
    }

    public Query WithVector(ReadOnlyMemory<float> vector)
    {
        Vector = vector;
        return this;
    }

    public Query InNamespace(string? indexNamespace)
    {
        Namespace = indexNamespace;
        return this;
    }

    public Query WithFilter(Dictionary<string, object>? filter)
    {
        Filter = filter;
        return this;
    }

    public Query WithSparseVector(SparseVectorData? sparseVector)
    {
        SparseVector = sparseVector;
        return this;
    }

    public Query WithId(string id)
    {
        Id = id;
        return this;
    }

    [JsonConstructor]
    private Query()
    {
    }
}
