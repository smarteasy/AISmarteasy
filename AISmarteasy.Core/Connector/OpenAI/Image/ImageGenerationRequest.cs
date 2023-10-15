using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connector.OpenAI.Image;

public sealed class ImageGenerationRequest
{
    [JsonPropertyName("prompt")]
    [JsonPropertyOrder(1)]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    [JsonPropertyOrder(2)]
    public string Size { get; set; } = "256x256";

    [JsonPropertyName("n")]
    [JsonPropertyOrder(3)]
    public int Count { get; set; } = 1;

    [JsonPropertyName("response_format")]
    [JsonPropertyOrder(4)]
    public string Format { get; set; } = "url";
}
