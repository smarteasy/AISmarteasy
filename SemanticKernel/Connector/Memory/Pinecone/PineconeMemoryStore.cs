using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Memory;

namespace SemanticKernel.Connector.Memory.Pinecone;


public class PineconeMemoryStore : IPineconeMemoryStore
{
    private readonly string _environment;

    public PineconeMemoryStore(
        string environment,
        string apiKey,
        ILoggerFactory? loggerFactory = null)
    {
        _environment = environment;
        _pineconeClient = new PineconeClient(environment, apiKey, loggerFactory);
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(PineconeMemoryStore)) : NullLogger.Instance;
    }

    public async Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (!await DoesCollectionExistAsync(collectionName, cancellationToken).ConfigureAwait(false))
        {
            throw new SKException("Index creation is not supported within memory store. " +
                $"It should be created manually or using {nameof(IPineconeClient.CreateIndexAsync)}. " +
                $"Ensure index state is {IndexState.Ready}.");
        }
    }

    public IList<string> GetCollectionsAsync()
    {
        return null;
        //await foreach (var index in _pineconeClient.ListIndexesAsync().ConfigureAwait(false))
        //{
        //    yield return index ?? "";
        //}
    }

    public async Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return await _pineconeClient.DoesIndexExistAsync(collectionName, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (await this.DoesCollectionExistAsync(collectionName, cancellationToken).ConfigureAwait(false))
        {
            await this._pineconeClient.DeleteIndexAsync(collectionName, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        return await this.UpsertToNamespaceAsync(collectionName, string.Empty, record, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> UpsertToNamespaceAsync(string indexName, string indexNamespace, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        (PineconeDocument vectorData, OperationTypeKind operationType) = await this.EvaluateAndUpdateMemoryRecordAsync(indexName, record, indexNamespace, cancellationToken).ConfigureAwait(false);

        Task request = operationType switch
        {
            OperationTypeKind.Upsert => this._pineconeClient.UpsertAsync(indexName, new[] { vectorData }, indexNamespace, cancellationToken),
            OperationTypeKind.Update => this._pineconeClient.UpdateAsync(indexName, vectorData, indexNamespace, cancellationToken),
            OperationTypeKind.Skip => Task.CompletedTask,
            _ => Task.CompletedTask
        };

        try
        {
            await request.ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to upsert: {Message}", ex.Message);
            throw;
        }

        return vectorData.Id;
    }

    public async IAsyncEnumerable<string> UpsertBatchAsync(
        string collectionName,
        IEnumerable<MemoryRecord> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (string id in this.UpsertBatchToNamespaceAsync(collectionName, string.Empty, records, cancellationToken).ConfigureAwait(false))
        {
            yield return id;
        }
    }

    public async IAsyncEnumerable<string> UpsertBatchToNamespaceAsync(
        string indexName,
        string indexNamespace,
        IEnumerable<MemoryRecord> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<PineconeDocument> upsertDocuments = new();
        List<PineconeDocument> updateDocuments = new();

        foreach (MemoryRecord? record in records)
        {
            (PineconeDocument document, OperationTypeKind operationType) = await this.EvaluateAndUpdateMemoryRecordAsync(
                indexName,
                record,
                indexNamespace,
                cancellationToken).ConfigureAwait(false);

            switch (operationType)
            {
                case OperationTypeKind.Upsert:
                    upsertDocuments.Add(document);
                    break;

                case OperationTypeKind.Update:

                    updateDocuments.Add(document);
                    break;

                case OperationTypeKind.Skip:
                    yield return document.Id;
                    break;
            }
        }

        List<Task> tasks = new();

        if (upsertDocuments.Count > 0)
        {
            tasks.Add(this._pineconeClient.UpsertAsync(indexName, upsertDocuments, indexNamespace, cancellationToken));
        }

        if (updateDocuments.Count > 0)
        {
            IEnumerable<Task> updates = updateDocuments.Select(async d
                => await this._pineconeClient.UpdateAsync(indexName, d, indexNamespace, cancellationToken).ConfigureAwait(false));

            tasks.AddRange(updates);
        }

        PineconeDocument[] vectorData = upsertDocuments.Concat(updateDocuments).ToArray();

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to upsert batch: {Message}", ex.Message);
            throw;
        }

        foreach (PineconeDocument? v in vectorData)
        {
            yield return v.Id;
        }
    }

    public async Task<MemoryRecord?> GetAsync(
        string collectionName,
        string key,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default)
    {
        return await this.GetFromNamespaceAsync(collectionName, string.Empty, key, withEmbedding, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MemoryRecord?> GetFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        string key,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (PineconeDocument? record in this._pineconeClient.FetchVectorsAsync(
                               indexName,
                               new[] { key },
                               indexNamespace,
                               withEmbedding,
                               cancellationToken))
            {
                return record?.ToMemoryRecord(transferVectorOwnership: true);
            }
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to get vector data from Pinecone: {Message}", ex.Message);
            throw;
        }

        return null;
    }

    public async IAsyncEnumerable<MemoryRecord> GetBatchAsync(
        string collectionName,
        IEnumerable<string> keys,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (MemoryRecord? record in this.GetBatchFromNamespaceAsync(collectionName, string.Empty, keys, withEmbeddings, cancellationToken).ConfigureAwait(false))
        {
            yield return record;
        }
    }

    public async IAsyncEnumerable<MemoryRecord> GetBatchFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        IEnumerable<string> keys,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (string? key in keys)
        {
            MemoryRecord? record = await this.GetFromNamespaceAsync(indexName, indexNamespace, key, withEmbeddings, cancellationToken).ConfigureAwait(false);

            if (record != null)
            {
                yield return record;
            }
        }
    }

    public async IAsyncEnumerable<MemoryRecord?> GetWithDocumentIdAsync(string indexName,
        string documentId,
        int limit = 3,
        string indexNamespace = "",
        bool withEmbedding = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (MemoryRecord? record in this.GetWithDocumentIdBatchAsync(indexName, new[] { documentId }, limit, indexNamespace, withEmbedding, cancellationToken).ConfigureAwait(false))
        {
            yield return record;
        }
    }

    public async IAsyncEnumerable<MemoryRecord?> GetWithDocumentIdBatchAsync(string indexName,
        IEnumerable<string> documentIds,
        int limit = 3,
        string indexNamespace = "",
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (IAsyncEnumerable<MemoryRecord?>? records
                 in documentIds.Select(
                     documentId => this.GetWithDocumentIdAsync(indexName, documentId, limit, indexNamespace, withEmbeddings, cancellationToken)))
        {
            await foreach (MemoryRecord? record in records.WithCancellation(cancellationToken))
            {
                yield return record;
            }
        }
    }

    public async IAsyncEnumerable<MemoryRecord?> GetBatchWithFilterAsync(string indexName,
        Dictionary<string, object> filter,
        int limit = 10,
        string indexNamespace = "",
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IEnumerable<PineconeDocument?> vectorDataList;

        try
        {
            Query query = Query.Create(limit)
                .InNamespace(indexNamespace)
                .WithFilter(filter);

            vectorDataList = await this._pineconeClient
                .QueryAsync(indexName,
                    query,
                    cancellationToken: cancellationToken)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Error getting batch with filter from Pinecone: {Message}", ex.Message);
            throw;
        }

        foreach (PineconeDocument? record in vectorDataList)
        {
            yield return record?.ToMemoryRecord(transferVectorOwnership: true);
        }
    }

    public async Task RemoveAsync(string collectionName, string key, CancellationToken cancellationToken = default)
    {
        await this.RemoveFromNamespaceAsync(collectionName, string.Empty, key, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveFromNamespaceAsync(string indexName, string indexNamespace, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await this._pineconeClient.DeleteAsync(indexName, new[]
                {
                    key
                },
                indexNamespace,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to remove vector data from Pinecone: {Message}", ex.Message);
            throw;
        }
    }

    public async Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        await this.RemoveBatchFromNamespaceAsync(collectionName, string.Empty, keys, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveBatchFromNamespaceAsync(string indexName, string indexNamespace, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(keys.Select(async k => await this.RemoveFromNamespaceAsync(indexName, indexNamespace, k, cancellationToken).ConfigureAwait(false))).ConfigureAwait(false);
    }

    public async Task RemoveWithFilterAsync(
        string indexName,
        Dictionary<string, object> filter,
        string indexNamespace = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await this._pineconeClient.DeleteAsync(
                indexName,
                default,
                indexNamespace,
                filter,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to remove vector data from Pinecone: {Message}", ex.Message);
            throw;
        }
    }

    public async Task RemoveWithDocumentIdAsync(string indexName, string documentId, string indexNamespace, CancellationToken cancellationToken = default)
    {
        try
        {
            await this._pineconeClient.DeleteAsync(indexName, null, indexNamespace, new Dictionary<string, object>()
            {
                { "document_Id", documentId }
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Failed to remove vector data from Pinecone: {Message}", ex.Message);
            throw;
        }
    }

    public async Task RemoveWithDocumentIdBatchAsync(
        string indexName,
        IEnumerable<string> documentIds,
        string indexNamespace,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<Task> tasks = documentIds.Select(async id
                => await this.RemoveWithDocumentIdAsync(indexName, id, indexNamespace, cancellationToken)
                    .ConfigureAwait(false));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            this._logger.LogError(ex, "Error in batch removing data from Pinecone: {Message}", ex.Message);
            throw;
        }
    }

    public IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(
        string collectionName,
        ReadOnlyMemory<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default)
    {
        return this.GetNearestMatchesFromNamespaceAsync(
            collectionName,
            string.Empty,
            embedding,
            limit,
            minRelevanceScore,
            withEmbeddings,
            cancellationToken);
    }

    public async IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        ReadOnlyMemory<float> embedding,
        int limit,
        double minRelevanceScore = 0,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<(PineconeDocument, double)> results = this._pineconeClient.GetMostRelevantAsync(
            indexName,
            embedding,
            minRelevanceScore,
            limit,
            withEmbeddings,
            true,
            indexNamespace,
            default,
            cancellationToken);

        await foreach ((PineconeDocument, double) result in results.WithCancellation(cancellationToken))
        {
            yield return (result.Item1.ToMemoryRecord(transferVectorOwnership: true), result.Item2);
        }
    }

    public async Task<(MemoryRecord, double)?> GetNearestMatchAsync(
        string collectionName,
        ReadOnlyMemory<float> embedding,
        double minRelevanceScore = 0,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default)
    {
        return await this.GetNearestMatchFromNamespaceAsync(
            collectionName,
            string.Empty,
            embedding,
            minRelevanceScore,
            withEmbedding,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<(MemoryRecord, double)?> GetNearestMatchFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        ReadOnlyMemory<float> embedding,
        double minRelevanceScore = 0,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<(MemoryRecord, double)> results = this.GetNearestMatchesFromNamespaceAsync(
            indexName,
            indexNamespace,
            embedding,
            minRelevanceScore: minRelevanceScore,
            limit: 1,
            withEmbeddings: withEmbedding,
            cancellationToken: cancellationToken);

        (MemoryRecord, double) record = await results.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return (record.Item1, record.Item2);
    }

    public async IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesWithFilterAsync(
        string indexName,
        ReadOnlyMemory<float> embedding,
        int limit,
        Dictionary<string, object> filter,
        double minRelevanceScore = 0D,
        string indexNamespace = "",
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<(PineconeDocument, double)> results = this._pineconeClient.GetMostRelevantAsync(
            indexName,
            embedding,
            minRelevanceScore,
            limit,
            withEmbeddings,
            true,
            indexNamespace,
            filter,
            cancellationToken);

        await foreach ((PineconeDocument, double) result in results.WithCancellation(cancellationToken))
        {
            yield return (result.Item1.ToMemoryRecord(transferVectorOwnership: true), result.Item2);
        }
    }

    public async Task ClearNamespaceAsync(string indexName, string indexNamespace, CancellationToken cancellationToken = default)
    {
        await this._pineconeClient.DeleteAsync(indexName, default, indexNamespace, null, true, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string?> ListNamespacesAsync(string indexName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IndexStats? indexStats = await this._pineconeClient.DescribeIndexStatsAsync(indexName, default, cancellationToken).ConfigureAwait(false);

        if (indexStats is null)
        {
            yield break;
        }

        foreach (string? indexNamespace in indexStats.Namespaces.Keys)
        {
            yield return indexNamespace;
        }
    }

    private readonly IPineconeClient _pineconeClient;
    private readonly ILogger _logger;

    private async Task<(PineconeDocument, OperationTypeKind)> EvaluateAndUpdateMemoryRecordAsync(
        string indexName,
        MemoryRecord record,
        string indexNamespace = "",
        CancellationToken cancellationToken = default)
    {
        string key = !string.IsNullOrEmpty(record.Key)
            ? record.Key
            : record.Metadata.Id;

        PineconeDocument vectorData = record.ToPineconeDocument();

        PineconeDocument? existingRecord = await this._pineconeClient.FetchVectorsAsync(indexName, new[] { key }, indexNamespace, false, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (existingRecord is null)
        {
            return (vectorData, OperationTypeKind.Upsert);
        }

        if (existingRecord.Metadata != null && vectorData.Metadata != null)
        {
            if (existingRecord.Metadata.SequenceEqual(vectorData.Metadata))
            {
                return (vectorData, OperationTypeKind.Skip);
            }
        }

        return (vectorData, OperationTypeKind.Update);
    }

}
