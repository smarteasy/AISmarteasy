namespace SemanticKernel.Memory;

public interface IPineconeMemoryStore : IMemoryStore
{
    Task<string> UpsertToNamespaceAsync(
        string indexName,
        string indexNamespace,
        MemoryRecord record,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> UpsertBatchToNamespaceAsync(
        string indexName,
        string indexNamespace,
        IEnumerable<MemoryRecord> records,
        CancellationToken cancellationToken = default);

    Task<MemoryRecord?> GetFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        string key,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<MemoryRecord> GetBatchFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        IEnumerable<string> keys,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<MemoryRecord?> GetWithDocumentIdAsync(
        string indexName,
        string documentId,
        int limit = 3,
        string indexNamespace = "",
        bool withEmbedding = false,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<MemoryRecord?> GetWithDocumentIdBatchAsync(
        string indexName,
        IEnumerable<string> documentIds,
        int limit = 3,
        string indexNamespace = "",
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    public IAsyncEnumerable<MemoryRecord?> GetBatchWithFilterAsync(
        string indexName,
        Dictionary<string, object> filter,
        int limit = 10,
        string indexNamespace = "",
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    Task RemoveFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        string key,
        CancellationToken cancellationToken = default);

    Task RemoveBatchFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default);

    Task RemoveWithDocumentIdAsync(
        string indexName,
        string documentId,
        string indexNamespace = "",
        CancellationToken cancellationToken = default);

    public Task RemoveWithDocumentIdBatchAsync(
        string indexName,
        IEnumerable<string> documentIds,
        string indexNamespace = "",
        CancellationToken cancellationToken = default);

    Task RemoveWithFilterAsync(
        string indexName,
        Dictionary<string, object> filter,
        string indexNamespace = "",
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesWithFilterAsync(
        string indexName,
        ReadOnlyMemory<float> embedding,
        int limit,
        Dictionary<string, object> filter,
        double minRelevanceScore = 0.0,
        string indexNamespace = "",
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        ReadOnlyMemory<float> embedding,
        int limit,
        double minRelevanceScore = 0.0,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    Task<(MemoryRecord, double)?> GetNearestMatchFromNamespaceAsync(
        string indexName,
        string indexNamespace,
        ReadOnlyMemory<float> embedding,
        double minRelevanceScore = 0.0,
        bool withEmbedding = false,
        CancellationToken cancellationToken = default);

    Task ClearNamespaceAsync(string indexName, string indexNamespace, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string?> ListNamespacesAsync(string indexName, CancellationToken cancellationToken = default);
}
