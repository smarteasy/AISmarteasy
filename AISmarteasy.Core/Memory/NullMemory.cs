namespace AISmarteasy.Core.Memory;

public sealed class NullMemory : ISemanticMemory
{
    private static readonly Task<string> EmptyStringTask = Task.FromResult(string.Empty);

    public static NullMemory Instance { get; } = new();

    public Task<string> SaveAsync(string collection, string text, string id,
        string? description = null, string? additionalMetadata = null, CancellationToken cancellationToken = default)
    {
        return EmptyStringTask;
    }

    public Task<string> SaveReferenceAsync(string collection, string text, string externalId, string externalSourceName,
        string? description = null, string? additionalMetadata = null, CancellationToken cancellationToken = default)
    {
        return EmptyStringTask;
    }

    public Task<MemoryQueryResult?> GetAsync(string collection, string key, bool withEmbedding = false, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MemoryQueryResult?>(null);
    }


    public Task RemoveAsync(string collection, string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }


    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(string collection, string query, int limit = 1,
        double minRelevanceScore = 0.0, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        return AsyncEnumerable.Empty<MemoryQueryResult>();
    }

    public Task<IList<string>> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    private NullMemory()
    {
    }
}
