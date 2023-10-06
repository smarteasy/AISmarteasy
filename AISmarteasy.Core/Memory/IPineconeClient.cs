using AISmarteasy.Core.Memory.Pinecone;

namespace AISmarteasy.Core.Memory;

public interface IPineconeClient
{
    IAsyncEnumerable<string?> ListIndexesAsync(CancellationToken cancellationToken = default);





    IAsyncEnumerable<PineconeDocument?> FetchVectorsAsync(string indexName, IEnumerable<string> ids, string indexNamespace = "",
        bool includeValues = false, CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<PineconeDocument?> QueryAsync(string indexName, Query query, bool includeValues = false,
        bool includeMetadata = true, CancellationToken cancellationToken = default);

    IAsyncEnumerable<(PineconeDocument, double)> GetMostRelevantAsync(string indexName, ReadOnlyMemory<float> vector, double threshold,
        int topK, bool includeValues, bool includeMetadata, string indexNamespace = "",
        Dictionary<string, object>? filter = default, CancellationToken cancellationToken = default);

    Task<int> UpsertAsync(string indexName, IEnumerable<PineconeDocument> vectors, string indexNamespace = "", CancellationToken cancellationToken = default);

    Task UpdateAsync(string indexName, PineconeDocument document, string indexNamespace = "", CancellationToken cancellationToken = default);

    Task DeleteAsync(string indexName, IEnumerable<string>? ids = null, string indexNamespace = "", Dictionary<string, object>? filter = null,
        bool deleteAll = false, CancellationToken cancellationToken = default);

    Task<IndexStats?> DescribeIndexStatsAsync(string indexName, Dictionary<string, object>? filter = default, CancellationToken cancellationToken = default);



    Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default);

    Task<bool> DoesIndexExistAsync(string indexName, CancellationToken cancellationToken = default);

    Task<PineconeIndex?> DescribeIndexAsync(string indexName, CancellationToken cancellationToken = default);

    Task CreateIndexAsync(IndexDefinition indexDefinition, CancellationToken cancellationToken = default);

    Task ConfigureIndexAsync(string indexName, int replicas = 1, PodType podType = PodType.P1X1, CancellationToken cancellationToken = default);
}
