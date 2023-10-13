using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Memory;

public static class EmbeddingGenerationExtensions
{
    public static async Task<ReadOnlyMemory<TEmbedding>> GenerateEmbeddingAsync<TValue, TEmbedding>
        (this IEmbeddingGeneration<TValue, TEmbedding> generator, TValue value, CancellationToken cancellationToken = default)
        where TEmbedding : unmanaged
    {
        Verify.NotNull(generator);
        return (await generator.GenerateEmbeddingsAsync(new[] { value }, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }
}
