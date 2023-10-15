using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.OpenAI;

public sealed class OpenAITextEmbeddingGeneration : OpenAIClientBase, IAIService
{
    public OpenAITextEmbeddingGeneration(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null
    ) : base(modelId, apiKey, organization, httpClient, loggerFactory)
    {
    }

    public override async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return await GetEmbeddingsAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
