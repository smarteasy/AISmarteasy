using System.ComponentModel;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.PluginFunction;
using AISmarteasy.Core.Handling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;

public sealed class WebFileDownloadSkill
{
    public const string FilePathParamName = "filePath";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public WebFileDownloadSkill() 
    {
    }

    public WebFileDownloadSkill(ILoggerFactory? loggerFactory = null) :
        this(new HttpClient(NonDisposableHttpClientHandler.Instance, false), loggerFactory)
    {
    }

    public WebFileDownloadSkill(HttpClient httpClient, ILoggerFactory? loggerFactory = null)
    {
        this._httpClient = httpClient;
        this._logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(WebFileDownloadSkill)) : NullLogger.Instance;
    }

    [SKFunction, Description("Downloads a file to local storage")]
    public async Task DownloadToFileAsync(
        [Description("URL of file to download")] Uri url,
        [Description("Path where to save file locally")] string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"{nameof(this.DownloadToFileAsync)} got called");

        _logger.LogDebug("Sending GET request for {0}", url);

        HttpRequestMessage request = new(HttpMethod.Get, url);
        HttpResponseMessage response = await _httpClient.SendWithSuccessCheckAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Response received: {0}", response.StatusCode);

        Stream webStream = await response.Content.ReadAsStreamAndTranslateExceptionAsync().ConfigureAwait(false);
        FileStream outputFileStream = new(Environment.ExpandEnvironmentVariables(filePath), FileMode.Create);

        await webStream.CopyToAsync(outputFileStream, 81920, cancellationToken).ConfigureAwait(false);
    }
}
