using System.Runtime.CompilerServices;
using SemanticKernel.Service;

namespace SemanticKernel.Memory;

public sealed class SemanticTextMemory : ISemanticTextMemory, IDisposable
{
    private readonly IAIService _embeddingService;
    private readonly IMemoryStore _storage;

    public SemanticTextMemory(IAIService embeddingService, IMemoryStore storage)
    {
        _embeddingService = embeddingService;
        _storage = storage;
    }

    public async Task<IList<string>> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        return await _storage.GetCollectionsAsync(cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
    }


    public async Task<string> SaveInformationAsync(string collection, string text, string id,
        string? description = null, string? additionalMetadata = null, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string> { text }, cancellationToken).ConfigureAwait(false);
        MemoryRecord data = MemoryRecord.LocalRecord(
            id: id, text: text, description: description, additionalMetadata: additionalMetadata, embedding: embeddings.First());

        if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).ConfigureAwait(false)))
        {
            await _storage.CreateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
        }

        return await _storage.UpsertAsync(collection, data, cancellationToken).ConfigureAwait(false);
    }






    public async Task<string> SaveReferenceAsync(
        string collection, string text, string externalId, string externalSourceName,
        string? description = null, string? additionalMetadata = null, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string>{ text }, cancellationToken).ConfigureAwait(false);
        var data = MemoryRecord.ReferenceRecord(externalId: externalId, sourceName: externalSourceName, description: description,
            additionalMetadata: additionalMetadata, embedding: embeddings.First());

        if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).ConfigureAwait(false)))
        {
            await _storage.CreateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
        }

        return await _storage.UpsertAsync(collection, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemoryQueryResult?> GetAsync(
        string collection,
        string key,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default)
    {
        MemoryRecord? record = await _storage.GetAsync(collection, key, withEmbedding, cancellationToken).ConfigureAwait(false);

        if (record == null) { return null; }

        return MemoryQueryResult.FromMemoryRecord(record, 1);
    }

    public async Task RemoveAsync(
        string collection,
        string key,
        CancellationToken cancellationToken = default)
    {
        await _storage.RemoveAsync(collection, key, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<MemoryQueryResult> SearchAsync(string collection, string query, 
        int limit = 1, double minRelevanceScore = 0.0, bool withEmbeddings = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryEmbeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string> { query }, cancellationToken).ConfigureAwait(false);

        var results = _storage.GetNearestMatchesAsync(collectionName: collection,
            embedding: queryEmbeddings.First(), limit: limit, minRelevanceScore: minRelevanceScore,
            withEmbeddings: withEmbeddings, cancellationToken: cancellationToken).ConfigureAwait(false);

        await foreach (var result in results.ConfigureAwait(false))
        {
            yield return MemoryQueryResult.FromMemoryRecord(result.Item1, result.Item2);
        }
    }

    public void Dispose()
    {
        if (_embeddingService is IDisposable emb) { emb.Dispose(); }
        if (_storage is IDisposable storage) { storage.Dispose(); }
    }
}
