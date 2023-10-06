using System.Text.Json.Serialization;

namespace AISmarteasy.Core;

public class AIRequestSettings
{
    /// <summary>
    /// Service identifier.
    /// </summary>
    [JsonPropertyName("service_id")]
    public string? ServiceId { get; set; } = null;

    /// <summary>
    /// Extra properties
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
}
