using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using SemanticKernel.Connector.Memory.Chroma;
using SemanticKernel.Web;

namespace SemanticKernel.Connector.Memory;

public class ChromaClient : IChromaClient
{
    public ChromaClient(string endpoint, ILoggerFactory? loggerFactory = null)
    {
        _httpClient = new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        _endpoint = endpoint;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(ChromaClient)) : NullLogger.Instance;
    }

    public ChromaClient(HttpClient httpClient, string? endpoint = null, ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(httpClient.BaseAddress?.AbsoluteUri) && string.IsNullOrEmpty(endpoint))
        {
            throw new SKException("The HttpClient BaseAddress and endpoint are both null or empty. Please ensure at least one is provided.");
        }

        _httpClient = httpClient;
        _endpoint = endpoint;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(ChromaClient)) : NullLogger.Instance;
    }

    public async Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating collection {0}", collectionName);

        using var request = CreateCollectionRequest.Create(collectionName).Build();

        await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ChromaCollectionModel?> GetCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        this._logger.LogDebug("Getting collection {0}", collectionName);

        using var request = GetCollectionRequest.Create(collectionName).Build();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var collection = JsonSerializer.Deserialize<ChromaCollectionModel>(responseContent);

        return collection;
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting collection {0}", collectionName);

        using var request = DeleteCollectionRequest.Create(collectionName).Build();

        await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> ListCollectionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing collections");

        using var request = ListCollectionsRequest.Create().Build();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var collections = JsonSerializer.Deserialize<List<ChromaCollectionModel>>(responseContent);

        foreach (var collection in collections!)
        {
            yield return collection.Name;
        }
    }

    public async Task UpsertEmbeddingsAsync(string collectionId, string[] ids, ReadOnlyMemory<float>[] embeddings, object[]? metadatas = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting embeddings to collection with id: {0}", collectionId);

        using var request = UpsertEmbeddingsRequest.Create(collectionId, ids, embeddings, metadatas).Build();

        await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ChromaEmbeddingsModel> GetEmbeddingsAsync(string collectionId, string[] ids, string[]? include = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting embeddings from collection with id: {0}", collectionId);

        using var request = GetEmbeddingsRequest.Create(collectionId, ids, include).Build();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var embeddings = JsonSerializer.Deserialize<ChromaEmbeddingsModel>(responseContent);

        return embeddings ?? new ChromaEmbeddingsModel();
    }

    public async Task DeleteEmbeddingsAsync(string collectionId, string[] ids, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting embeddings from collection with id: {0}", collectionId);

        using var request = DeleteEmbeddingsRequest.Create(collectionId, ids).Build();

        await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ChromaQueryResultModel> QueryEmbeddingsAsync(string collectionId, ReadOnlyMemory<float>[] queryEmbeddings, int nResults, string[]? include = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Query embeddings in collection with id: {0}", collectionId);

        using var request = QueryEmbeddingsRequest.Create(collectionId, queryEmbeddings, nResults, include).Build();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var queryResult = JsonSerializer.Deserialize<ChromaQueryResultModel>(responseContent);

        return queryResult ?? new ChromaQueryResultModel();
    }

    private const string API_ROUTE = "api/v1/";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint = null;

    private async Task<(HttpResponseMessage response, string responseContent)> ExecuteHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        string endpoint = _endpoint ?? _httpClient.BaseAddress.ToString();
        endpoint = SanitizeEndpoint(endpoint);

        string operationName = request.RequestUri.ToString();

        request.RequestUri = new Uri(new Uri(endpoint), operationName);

        HttpResponseMessage? response;

        string? responseContent;

        try
        {
            response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);

            responseContent = await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);
        }
        catch (HttpOperationException e)
        {
            _logger.LogError(e, "{Method} {Path} operation failed: {Message}, {Response}", request.Method.Method, operationName, e.Message, e.ResponseContent);
            throw;
        }

        return (response, responseContent);
    }

    private string SanitizeEndpoint(string endpoint)
    {
        StringBuilder builder = new(endpoint);

        if (!endpoint.EndsWith("/", StringComparison.Ordinal))
        {
            builder.Append('/');
        }

        builder.Append(API_ROUTE);

        return builder.ToString();
    }
}
