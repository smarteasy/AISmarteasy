using SemanticKernel.Connector.OpenAI.TextCompletion;

namespace SemanticKernel.Embedding;

public interface ITextEmbeddingGeneration : IEmbeddingGeneration<string, float>, IAIService
{
}
