namespace AISmarteasy.Core.Service;

public interface IEmbeddingGeneration : IAIService
{
    Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data,  CancellationToken cancellationToken = default);
}
