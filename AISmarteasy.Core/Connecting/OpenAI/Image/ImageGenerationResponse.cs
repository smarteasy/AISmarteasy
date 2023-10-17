using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.OpenAI.Image;

public class ImageGenerationResponse
{
    public sealed class Image
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("b64_json")]
        public string AsBase64 { get; set; } = string.Empty;
    }

    [JsonPropertyName("data")]
    public IList<Image> Images { get; set; } = new List<Image>();

    [JsonPropertyName("created")]
    public int CreatedTime { get; set; }
}
