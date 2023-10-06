using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AISmarteasy.Core.Memory.Pinecone;
using AISmarteasy.Core.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Memory;

public sealed class PineconeClient : IPineconeClient
{
    public PineconeClient(string environment, string apiKey, ILoggerFactory? loggerFactory = null)
    {
        _environment = environment;
        _authHeader = new KeyValuePair<string, string>("Api-Key", apiKey);
        _jsonSerializerOptions = PineconeUtils.DefaultSerializerOptions;
        _indexHostMapping = new ConcurrentDictionary<string, string>();

        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(PineconeClient)) : NullLogger.Instance;

        _httpClient = new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
    }

    public async IAsyncEnumerable<string?> ListIndexesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = ListIndexesRequest.Create().Build();

        (HttpResponseMessage _, string responseContent) = await ExecuteHttpRequestAsync(GetIndexOperationsApiBasePath(), request, cancellationToken).ConfigureAwait(false);

        string[]? indices = JsonSerializer.Deserialize<string[]?>(responseContent, _jsonSerializerOptions);

        if (indices == null)
        {
            yield break;
        }

        foreach (string? index in indices)
        {
            yield return index;
        }
    }

    public async Task<IndexStats?> DescribeIndexStatsAsync(
        string indexName,
        Dictionary<string, object>? filter = default,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting index stats for index {0}", indexName);

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);

        using HttpRequestMessage request = DescribeIndexStatsRequest.GetIndexStats()
            .WithFilter(filter)
            .Build();

        string? responseContent = null;

        try
        {
            (_, responseContent) = await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Index not found {Message}", e.Message);
            throw;
        }

        IndexStats? result = JsonSerializer.Deserialize<IndexStats>(responseContent, _jsonSerializerOptions);

        if (result != null)
        {
            _logger.LogDebug("Index stats retrieved");
        }
        else
        {
            _logger.LogWarning("Index stats retrieval failed");
        }

        return result;
    }

    private string GetIndexOperationsApiBasePath()
    {
        return $"https://controller.{_environment}.pinecone.io";
    }

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        string baseUrl,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add(_authHeader.Key, _authHeader.Value);
        request.RequestUri = new Uri(baseUrl + request.RequestUri);

        var response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);

        return (response, responseContent);
    }

    public async Task CreateIndexAsync(IndexDefinition indexDefinition, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating index {0}", indexDefinition.ToString());

        string indexName = indexDefinition.Name;

        using HttpRequestMessage request = indexDefinition.Build();

        try
        {
            await ExecuteHttpRequestAsync(GetIndexOperationsApiBasePath(), request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError(e, "Bad Request: {StatusCode}, {Response}", e.StatusCode, e.ResponseContent);
            throw;
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogError(e, "Index of given name already exists: {StatusCode}, {Response}", e.StatusCode, e.ResponseContent);
            throw;
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Creating index failed: {Message}, {Response}", e.Message, e.ResponseContent);
            throw;
        }
    }









    public async IAsyncEnumerable<PineconeDocument?> FetchVectorsAsync(
        string indexName,
        IEnumerable<string> ids,
        string indexNamespace = "",
        bool includeValues = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching vectors by id");

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);

        FetchRequest fetchRequest = FetchRequest.FetchVectors(ids).FromNamespace(indexNamespace);

        using HttpRequestMessage request = fetchRequest.Build();

        string? responseContent;

        try
        {
            (_, responseContent) = await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Error occurred on Get Vectors request: {Message}", e.Message);
            yield break;
        }

        FetchResponse? data = JsonSerializer.Deserialize<FetchResponse>(responseContent, _jsonSerializerOptions);

        if (data == null)
        {
            _logger.LogWarning("Unable to deserialize Get response");
            yield break;
        }

        if (data.Vectors.Count == 0)
        {
            _logger.LogWarning("Vectors not found");
            yield break;
        }

        IEnumerable<PineconeDocument> records = includeValues
            ? data.Vectors.Values
            : data.WithoutEmbeddings();

        foreach (PineconeDocument? record in records)
        {
            yield return record;
        }
    }

    public async IAsyncEnumerable<PineconeDocument?> QueryAsync(
        string indexName,
        Query query,
        bool includeValues = false,
        bool includeMetadata = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying top {0} nearest vectors", query.TopK);

        using HttpRequestMessage request = QueryRequest.QueryIndex(query)
            .WithMetadata(includeMetadata)
            .WithEmbeddings(includeValues)
            .Build();

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);

        string? responseContent;

        try
        {
            (_, responseContent) = await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Error occurred on Query Vectors request: {Message}", e.Message);
            yield break;
        }

        QueryResponse? queryResponse = JsonSerializer.Deserialize<QueryResponse>(responseContent, _jsonSerializerOptions);

        if (queryResponse == null)
        {
            _logger.LogWarning("Unable to deserialize Query response");
            yield break;
        }

        if (queryResponse.Matches.Count == 0)
        {
            _logger.LogWarning("No matches found");
            yield break;
        }

        foreach (PineconeDocument? match in queryResponse.Matches)
        {
            yield return match;
        }
    }

    public async IAsyncEnumerable<(PineconeDocument, double)> GetMostRelevantAsync(
        string indexName,
        ReadOnlyMemory<float> vector,
        double threshold,
        int topK,
        bool includeValues,
        bool includeMetadata,
        string indexNamespace = "",
        Dictionary<string, object>? filter = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching top {0} nearest vectors with threshold {1}", topK, threshold);

        List<(PineconeDocument document, float score)> documents = new();

        Query query = Query.Create(topK)
            .WithVector(vector)
            .InNamespace(indexNamespace)
            .WithFilter(filter);

        IAsyncEnumerable<PineconeDocument?> matches = QueryAsync(
            indexName, query,
            includeValues,
            includeMetadata, cancellationToken);

        await foreach (PineconeDocument? match in matches.WithCancellation(cancellationToken))
        {
            if (match == null)
            {
                continue;
            }

            if (match.Score > threshold)
            {
                documents.Add((match, match.Score ?? 0));
            }
        }

        if (documents.Count == 0)
        {
            _logger.LogWarning("No relevant documents found");
            yield break;
        }

        documents = documents.OrderByDescending(x => x.score).ToList();

        foreach ((PineconeDocument document, float score) in documents)
        {
            yield return (document, score);
        }
    }

    public async Task<int> UpsertAsync(
        string indexName,
        IEnumerable<PineconeDocument> vectors,
        string indexNamespace = "",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting vectors");

        int totalUpserted = 0;
        int totalBatches = 0;

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);
        IAsyncEnumerable<PineconeDocument> validVectors = PineconeUtils.EnsureValidMetadataAsync(vectors.ToAsyncEnumerable());

        await foreach (UpsertRequest? batch in PineconeUtils.GetUpsertBatchesAsync(validVectors, MAX_BATCH_SIZE).WithCancellation(cancellationToken))
        {
            totalBatches++;

            using HttpRequestMessage request = batch.ToNamespace(indexNamespace).Build();

            string? responseContent;

            try
            {
                (_, responseContent) = await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpOperationException e)
            {
                _logger.LogError(e, "Failed to upsert vectors {Message}", e.Message);
                throw;
            }

            UpsertResponse? data = JsonSerializer.Deserialize<UpsertResponse>(responseContent, _jsonSerializerOptions);

            if (data == null)
            {
                _logger.LogWarning("Unable to deserialize Upsert response");
                continue;
            }

            totalUpserted += data.UpsertedCount;

            _logger.LogDebug("Upserted batch {0} with {1} vectors", totalBatches, data.UpsertedCount);
        }

        _logger.LogDebug("Upserted {0} vectors in {1} batches", totalUpserted, totalBatches);

        return totalUpserted;
    }

    public async Task DeleteAsync(
        string indexName,
        IEnumerable<string>? ids = null,
        string indexNamespace = "",
        Dictionary<string, object>? filter = null,
        bool deleteAll = false,
        CancellationToken cancellationToken = default)
    {
        if (ids == null && string.IsNullOrEmpty(indexNamespace) && filter == null && !deleteAll)
        {
            throw new SKException("Must provide at least one of ids, filter, or deleteAll");
        }

        ids = ids?.ToList();

        DeleteRequest deleteRequest = deleteAll
            ? string.IsNullOrEmpty(indexNamespace)
                ? DeleteRequest.GetDeleteAllVectorsRequest()
                : DeleteRequest.ClearNamespace(indexNamespace)
            : DeleteRequest.DeleteVectors(ids)
                .FromNamespace(indexNamespace)
                .FilterBy(filter);

        _logger.LogDebug("Delete operation for Index {0}: {1}", indexName, deleteRequest.ToString());

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);

        using HttpRequestMessage request = deleteRequest.Build();

        try
        {
            await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Delete operation failed: {Message}", e.Message);
            throw;
        }
    }

    public async Task UpdateAsync(string indexName, PineconeDocument document, string indexNamespace = "", CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating vector: {0}", document.Id);

        string basePath = await GetVectorOperationsApiBasePathAsync(indexName).ConfigureAwait(false);

        using HttpRequestMessage request = UpdateVectorRequest
            .FromPineconeDocument(document)
            .InNamespace(indexNamespace)
            .Build();

        try
        {
            await ExecuteHttpRequestAsync(basePath, request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Vector update for Document {Id} failed. {Message}", document.Id, e.Message);
            throw;
        }
    }



    public async Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting index {0}", indexName);

        using HttpRequestMessage request = DeleteIndexRequest.Create(indexName).Build();

        try
        {
            await ExecuteHttpRequestAsync(GetIndexOperationsApiBasePath(), request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError(e, "Index Not Found: {StatusCode}, {Response}", e.StatusCode, e.ResponseContent);
            throw;
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Deleting index failed: {Message}, {Response}", e.Message, e.ResponseContent);
            throw;
        }

        _logger.LogDebug("Index: {0} has been successfully deleted.", indexName);
    }

    public async Task<bool> DoesIndexExistAsync(string indexName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for index {0}", indexName);

        List<string?>? indexNames = await ListIndexesAsync(cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (indexNames.All(name => name != indexName))
        {
            return false;
        }

        PineconeIndex? index = await DescribeIndexAsync(indexName, cancellationToken).ConfigureAwait(false);

        return index != null && index.Status.State == IndexState.Ready;
    }

    public async Task<PineconeIndex?> DescribeIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting Description for Index: {0}", indexName);

        using HttpRequestMessage request = DescribeIndexRequest.Create(indexName).Build();

        string? responseContent;

        try
        {
            (_, responseContent) = await ExecuteHttpRequestAsync(GetIndexOperationsApiBasePath(), request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError(e, "Bad Request: {StatusCode}, {Response}", e.StatusCode, e.ResponseContent);
            throw;
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Describe index failed: {Message}, {Response}", e.Message, e.ResponseContent);
            throw;
        }

        PineconeIndex? indexDescription = null;
        try
        {
            indexDescription = JsonSerializer.Deserialize<PineconeIndex>(responseContent, _jsonSerializerOptions);
        }
        catch (Exception e)
        {
            _logger.LogDebug("JsonSerialization raise Exception.");
        }

        if (indexDescription == null)
        {
            _logger.LogDebug("Deserialized index description is null");
        }

        return indexDescription;
    }

    public async Task ConfigureIndexAsync(string indexName, int replicas = 1, PodType podType = PodType.P1X1, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Configuring index {0}", indexName);

        using HttpRequestMessage request = ConfigureIndexRequest
            .Create(indexName)
            .WithPodType(podType)
            .NumberOfReplicas(replicas)
            .Build();

        try
        {
            await ExecuteHttpRequestAsync(GetIndexOperationsApiBasePath(), request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError(e, "Request exceeds quota or collection name is invalid. {Index}", indexName);
            throw;
        }
        catch (HttpOperationException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError(e, "Index not found. {Index}", indexName);
            throw;
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "Index configuration failed: {Message}, {Response}", e.Message, e.ResponseContent);
            throw;
        }

        _logger.LogDebug("Collection created. {0}", indexName);
    }

    private readonly string _environment;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    private readonly KeyValuePair<string, string> _authHeader;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<string, string> _indexHostMapping;
    private const int MAX_BATCH_SIZE = 100;

    private async Task<string> GetVectorOperationsApiBasePathAsync(string indexName)
    {
        string indexHost = await GetIndexHostAsync(indexName).ConfigureAwait(false);

        return $"https://{indexHost}";
    }



    private async Task<string> GetIndexHostAsync(string indexName, CancellationToken cancellationToken = default)
    {
        if (_indexHostMapping.TryGetValue(indexName, out string? indexHost))
        {
            return indexHost;
        }

        _logger.LogDebug("Getting index host from Pinecone.");

        PineconeIndex? pineconeIndex = await DescribeIndexAsync(indexName, cancellationToken).ConfigureAwait(false);

        if (pineconeIndex == null)
        {
            throw new SKException("Index not found in Pinecone. Create index to perform operations with vectors.");
        }

        if (string.IsNullOrWhiteSpace(pineconeIndex.Status.Host))
        {
            throw new SKException($"Host of index {indexName} is unknown.");
        }

        _logger.LogDebug("Found host {0} for index {1}", pineconeIndex.Status.Host, indexName);

        _indexHostMapping.TryAdd(indexName, pineconeIndex.Status.Host);

        return pineconeIndex.Status.Host;
    }
}
