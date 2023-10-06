using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory;

public class DataEntryBase
{
    [JsonConstructor]
    public DataEntryBase(string? key = null, DateTimeOffset? timestamp = null)
    {
        this.Key = key ?? string.Empty;
        this.Timestamp = timestamp;
    }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonIgnore]
    public bool HasTimestamp => this.Timestamp.HasValue;
}
