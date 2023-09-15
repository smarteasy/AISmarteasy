using SemanticKernel.Service;

namespace SemanticKernel.Embedding;

public interface ITextEmbeddingGeneration : IEmbeddingGeneration<string, float>, IAIService
{
}
