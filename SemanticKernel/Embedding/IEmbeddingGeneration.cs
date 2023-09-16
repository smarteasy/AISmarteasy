using SemanticKernel.Connector.OpenAI.TextCompletion;

namespace SemanticKernel.Embedding;

public interface IEmbeddingGeneration<TValue, TEmbedding> : IAIService
    where TEmbedding : unmanaged
{
    Task<IList<ReadOnlyMemory<TEmbedding>>> GenerateEmbeddingsAsync(IList<TValue> data, CancellationToken cancellationToken = default);
}
