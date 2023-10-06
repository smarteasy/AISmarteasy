using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Service;
namespace SemanticKernel.Connector.OpenAI;

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
        return await InternalGetEmbeddingsAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
