using System.Diagnostics.CodeAnalysis;
using SemanticKernel.Embedding;
using SemanticKernel.Function;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using NS of IKernel
namespace SemanticKernel.Memory;
#pragma warning restore IDE0130

public static class MemoryConfiguration
{
    public static void UseMemory(this IKernel kernel, IMemoryStore storage, string? embeddingsServiceId = null)
    {
        var embeddingGenerator = kernel.GetService<ITextEmbeddingGeneration>(embeddingsServiceId);

        UseMemory(kernel, embeddingGenerator, storage);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The embeddingGenerator object is disposed by the kernel")]
    public static void UseMemory(this IKernel kernel, ITextEmbeddingGeneration embeddingGenerator, IMemoryStore storage)
    {
        Verify.NotNull(storage);
        Verify.NotNull(embeddingGenerator);

        kernel.RegisterMemory(new SemanticTextMemory(storage, embeddingGenerator));
    }
}
