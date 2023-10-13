using AISmarteasy.Core.Function;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Connector.Web;

public sealed class GoogleConnector : IWebSearchEngineConnector, IDisposable
{
    private readonly ILogger _logger;
    private readonly CustomSearchAPIService _search;
    private readonly string? _searchEngineId;

    public GoogleConnector(
        string apiKey,
        string searchEngineId,
        ILoggerFactory? loggerFactory = null) : this(new BaseClientService.Initializer { ApiKey = apiKey }, searchEngineId, loggerFactory)
    {
        Verify.NotNullOrWhitespace(apiKey);
    }

    public GoogleConnector(
        BaseClientService.Initializer initializer,
        string searchEngineId,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(initializer);
        Verify.NotNullOrWhitespace(searchEngineId);

        _search = new CustomSearchAPIService(initializer);
        _searchEngineId = searchEngineId;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(GoogleConnector)) : NullLogger.Instance;
    }

    public async Task<IEnumerable<string>> SearchAsync(
        string query,
        int count,
        int offset,
        CancellationToken cancellationToken)
    {
        if (count <= 0) { throw new ArgumentOutOfRangeException(nameof(count)); }

        if (count > 10) { throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} value must be between 0 and 10, inclusive."); }

        if (offset < 0) { throw new ArgumentOutOfRangeException(nameof(offset)); }

        var search = _search.Cse.List();
        search.Cx = _searchEngineId;
        search.Q = query;
        search.Num = count;
        search.Start = offset;

        var results = await search.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return results.Items.Select(item => item.Snippet);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _search.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
