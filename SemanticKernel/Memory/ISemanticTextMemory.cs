namespace SemanticKernel.Memory;

public interface ISemanticTextMemory
{
    public Task<string> SaveInformationAsync(
        string collection,
        string text,
        string id,
        string? description = null,
        string? additionalMetadata = null,
        CancellationToken cancellationToken = default);

    public Task<string> SaveReferenceAsync(
        string collection,
        string text,
        string externalId,
        string externalSourceName,
        string? description = null,
        string? additionalMetadata = null,
        CancellationToken cancellationToken = default);

    public Task<MemoryQueryResult?> GetAsync(string collection, string key, bool withEmbedding = false, CancellationToken cancellationToken = default);

    public Task RemoveAsync(string collection, string key, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string collection,
        string query,
        int limit = 1,
        double minRelevanceScore = 0.7,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = default);

    public Task<IList<string>> GetCollectionsAsync(CancellationToken cancellationToken = default);
}
