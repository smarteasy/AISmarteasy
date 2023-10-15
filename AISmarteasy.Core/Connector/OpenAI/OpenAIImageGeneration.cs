using System.Diagnostics;
using AISmarteasy.Core.Connector.OpenAI.Image;
using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.OpenAI;

public class OpenAIImageGeneration : OpenAIClientBase, IAIService
{
    private const string OPEN_AI_ENDPOINT = "https://api.openai.com/v1/images/generations";

    private readonly string? _organizationHeaderValue;

    private readonly string _authorizationHeaderValue;

   public OpenAIImageGeneration(
        string apiKey, 
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null
    ) : base(httpClient, loggerFactory)
    {
        Verify.NotNullOrWhitespace(apiKey);
        _authorizationHeaderValue = $"Bearer {apiKey}";
        _organizationHeaderValue = organization;
    }

    private protected override void AddRequestHeaders(HttpRequestMessage request)
    {
        base.AddRequestHeaders(request);

        request.Headers.Add("Authorization", this._authorizationHeaderValue);
        if (!string.IsNullOrEmpty(this._organizationHeaderValue))
        {
            request.Headers.Add("OpenAI-Organization", this._organizationHeaderValue);
        }
    }

    public Task<string?> GenerateImageAsync(string description, int width, int height,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(description);
        if (width != height || (width != 256 && width != 512 && width != 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "OpenAI can generate only square images of size 256x256, 512x512, or 1024x1024.");
        }

        return GenerateImageAsync(description, width, height, "url", x => x.Url, cancellationToken);
    }

    private async Task<string?> GenerateImageAsync(string description, int width, int height,
        string format, Func<ImageGenerationResponse.Image, string> extractResponse, CancellationToken cancellationToken)
    {
        Debug.Assert(width == height);
        Debug.Assert(width is 256 or 512 or 1024);
        Debug.Assert(format is "url" or "b64_json");
        Debug.Assert(extractResponse is not null);

        var requestBody = Json.Serialize(new ImageGenerationRequest
        {
            Prompt = description,
            Size = $"{width}x{height}",
            Count = 1,
            Format = format,
        });

        var list = await GenerateImageGenerationAsync(OPEN_AI_ENDPOINT, requestBody, extractResponse!, cancellationToken).ConfigureAwait(false);
        return list?[0];
    }
}
