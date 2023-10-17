using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Pinecone;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IndexState
{
    None = 0,

    [EnumMember(Value = "Initializing")]
    Initializing = 1,

    [EnumMember(Value = "ScalingUp")]
    ScalingUp = 2,

    [EnumMember(Value = "ScalingDown")]
    ScalingDown = 3,

    [EnumMember(Value = "Terminating")]
    Terminating = 4,

    [EnumMember(Value = "Ready")]
    Ready = 5,
}
