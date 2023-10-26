using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connecting.OpenAI;

public sealed class OpenAIEmbeddingGeneration : OpenAIClientBase, IEmbeddingGeneration
{
    public OpenAIEmbeddingGeneration(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null
    ) : base(modelId, apiKey, AIServiceTypeKind.EmbeddingGeneration, organization, httpClient, loggerFactory)
    {
    }

    public override async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return await GetEmbeddingsAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
