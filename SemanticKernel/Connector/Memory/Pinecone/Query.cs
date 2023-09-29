using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

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
        this.Vector = vector;
        return this;
    }

    public Query InNamespace(string? indexNamespace)
    {
        this.Namespace = indexNamespace;
        return this;
    }

    public Query WithFilter(Dictionary<string, object>? filter)
    {
        this.Filter = filter;
        return this;
    }

    public Query WithSparseVector(SparseVectorData? sparseVector)
    {
        this.SparseVector = sparseVector;
        return this;
    }

    public Query WithId(string id)
    {
        this.Id = id;
        return this;
    }

    [JsonConstructor]
    private Query()
    {
    }
}
