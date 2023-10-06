using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory;

internal sealed class ReadOnlyMemoryConverter : JsonConverter<ReadOnlyMemory<float>>
{
    private static readonly JsonConverter<float[]> ArrayConverter = (JsonConverter<float[]>)new JsonSerializerOptions().GetConverter(typeof(float[]));

    public override ReadOnlyMemory<float> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ArrayConverter.Read(ref reader, typeof(float[]), options).AsMemory();

    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<float> value, JsonSerializerOptions options) =>
        ArrayConverter.Write(writer, MemoryMarshal.TryGetArray(value, out ArraySegment<float> array) &&
                                     array.Count == value.Length ? array.Array! : value.ToArray(), options);
}
