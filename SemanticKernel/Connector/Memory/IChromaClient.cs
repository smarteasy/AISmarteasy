using SemanticKernel.Connector.Memory.Chroma;

namespace SemanticKernel.Connector.Memory;


public interface IChromaClient
{
    Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ListCollectionsAsync(CancellationToken cancellationToken = default);

    Task<ChromaCollectionModel?> GetCollectionAsync(string collectionName, CancellationToken cancellationToken = default);

    Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);

    Task UpsertEmbeddingsAsync(string collectionId, string[] ids, ReadOnlyMemory<float>[] embeddings, object[]? metadatas = null, 
        CancellationToken cancellationToken = default);

    Task<ChromaEmbeddingsModel> GetEmbeddingsAsync(string collectionId, string[] ids, string[]? include = null, 
        CancellationToken cancellationToken = default);

   Task DeleteEmbeddingsAsync(string collectionId, string[] ids, CancellationToken cancellationToken = default);

    Task<ChromaQueryResultModel> QueryEmbeddingsAsync(string collectionId, ReadOnlyMemory<float>[] queryEmbeddings, int nResults, string[]? include = null, CancellationToken cancellationToken = default);
}
