﻿using System.Runtime.CompilerServices;
using SemanticKernel.Embedding;

namespace SemanticKernel.Memory;

public sealed class SemanticTextMemory : ISemanticTextMemory, IDisposable
{
    private readonly ITextEmbeddingGeneration _embeddingGenerator;
    private readonly IMemoryStore _storage;

    public SemanticTextMemory(ITextEmbeddingGeneration embeddingGenerator, IMemoryStore storage)
    {
        _embeddingGenerator = embeddingGenerator;
        _storage = storage;
    }

    public Task<IList<string>> GetCollectionsAsync()
    {
        return null;//await this._storage.GetCollectionsAsync().ToListAsync().ConfigureAwait(false);
    }





    public async Task<string> SaveInformationAsync(
        string collection,
        string text,
        string id,
        string? description = null,
        string? additionalMetadata = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
        MemoryRecord data = MemoryRecord.LocalRecord(
            id: id, text: text, description: description, additionalMetadata: additionalMetadata, embedding: embedding);

        if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).ConfigureAwait(false)))
        {
            await _storage.CreateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
        }

        return await _storage.UpsertAsync(collection, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> SaveReferenceAsync(
        string collection,
        string text,
        string externalId,
        string externalSourceName,
        string? description = null,
        string? additionalMetadata = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
        var data = MemoryRecord.ReferenceRecord(externalId: externalId, sourceName: externalSourceName, description: description,
            additionalMetadata: additionalMetadata, embedding: embedding);

        //if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).ConfigureAwait(false)))
        //{
            await _storage.CreateCollectionAsync(collection, cancellationToken).ConfigureAwait(false);
        //}

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

    public async IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string collection,
        string query,
        int limit = 1,
        double minRelevanceScore = 0.0,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<float> queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, cancellationToken).ConfigureAwait(false);

        IAsyncEnumerable<(MemoryRecord, double)> results = this._storage.GetNearestMatchesAsync(
            collectionName: collection,
            embedding: queryEmbedding,
            limit: limit,
            minRelevanceScore: minRelevanceScore,
            withEmbeddings: withEmbeddings,
            cancellationToken: cancellationToken);

        await foreach ((MemoryRecord, double) result in results.WithCancellation(cancellationToken))
        {
            yield return MemoryQueryResult.FromMemoryRecord(result.Item1, result.Item2);
        }
    }


    public void Dispose()
    {
        if (this._embeddingGenerator is IDisposable emb) { emb.Dispose(); }
        if (this._storage is IDisposable storage) { storage.Dispose(); }
    }
}
