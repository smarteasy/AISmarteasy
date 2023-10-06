using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory.Pinecone;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IndexMetric
{
    None = 0,

    [EnumMember(Value = "euclidean")]
    Euclidean = 1,

    [EnumMember(Value = "cosine")]
    Cosine = 2,

    [EnumMember(Value = "dotproduct")]
    Dotproduct = 3
}
