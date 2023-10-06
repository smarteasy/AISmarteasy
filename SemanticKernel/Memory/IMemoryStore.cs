namespace SemanticKernel.Memory;

public interface IMemoryStore
{
    IAsyncEnumerable<string> GetCollectionsAsync(CancellationToken cancellationToken = default);

    Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
    

    Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancellationToken = default);

   Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);

    Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancellationToken = default);

    IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(string collectionName, ReadOnlyMemory<float> embedding, int limit,
        double minRelevanceScore = 0.0, bool withEmbeddings = false, CancellationToken cancellationToken = default);






    IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records, CancellationToken cancellationToken = default);

    Task<MemoryRecord?> GetAsync(string collectionName, string key, bool withEmbedding = false, CancellationToken cancellationToken = default);

    IAsyncEnumerable<MemoryRecord> GetBatchAsync(string collectionName, IEnumerable<string> keys, bool withEmbeddings = false, CancellationToken cancellationToken = default);

     Task RemoveAsync(string collectionName, string key, CancellationToken cancellationToken = default);

     Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancellationToken = default);



    Task<(MemoryRecord, double)?> GetNearestMatchAsync(string collectionName, ReadOnlyMemory<float> embedding,
        double minRelevanceScore = 0.0, bool withEmbedding = false, CancellationToken cancellationToken = default);
}
