using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Embedding;

namespace SemanticKernel.Connector.OpenAI;

public sealed class OpenAITextEmbeddingGeneration : OpenAIClientBase, ITextEmbeddingGeneration
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
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return InternalGetEmbeddingsAsync(data, cancellationToken);
    }
}
