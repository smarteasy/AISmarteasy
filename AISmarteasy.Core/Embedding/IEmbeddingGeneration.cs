﻿using AISmarteasy.Core.Service;

namespace AISmarteasy.Core.Embedding;

public interface IEmbeddingGeneration<TValue, TEmbedding> : IAIService
    where TEmbedding : unmanaged
{
    Task<IList<ReadOnlyMemory<TEmbedding>>> GenerateEmbeddingsAsync(IList<TValue> data, CancellationToken cancellationToken = default);
}
