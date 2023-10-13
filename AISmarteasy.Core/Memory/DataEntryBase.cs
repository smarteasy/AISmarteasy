using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory;

public class DataEntryBase
{
    [JsonConstructor]
    public DataEntryBase(string? key = null, DateTimeOffset? timestamp = null)
    {
        Key = key ?? string.Empty;
        Timestamp = timestamp;
    }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonIgnore]
    public bool HasTimestamp => Timestamp.HasValue;
}
