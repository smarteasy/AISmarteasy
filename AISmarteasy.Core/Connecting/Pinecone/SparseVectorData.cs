using System.Text.Json.Serialization;
using AISmarteasy.Core.Memory;

namespace AISmarteasy.Core.Connecting.Pinecone;

public class SparseVectorData
{
    [JsonPropertyName("indices")]
    public IEnumerable<long> Indices { get; set; }


    [JsonPropertyName("values")]
    [JsonConverter(typeof(ReadOnlyMemoryConverter))]
    public ReadOnlyMemory<float> Values { get; set; }

    public static SparseVectorData CreateSparseVectorData(List<long> indices, ReadOnlyMemory<float> values)
    {
        return new SparseVectorData(indices, values);
    }

    [JsonConstructor]
    public SparseVectorData(List<long> indices, ReadOnlyMemory<float> values)
    {
        Indices = indices;
        Values = values;
    }
}
