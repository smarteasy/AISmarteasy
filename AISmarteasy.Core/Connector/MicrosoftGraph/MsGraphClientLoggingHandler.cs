using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class MsGraphClientLoggingHandler : DelegatingHandler
{
    private const string CLIENT_REQUEST_ID_HEADER_NAME = "client-request-id";

    private readonly List<string> _headerNamesToLog = new()
    {
        CLIENT_REQUEST_ID_HEADER_NAME,
        "request-id",
        "x-ms-ags-diagnostic",
        "Date"
    };

    private readonly ILogger _logger;

    public MsGraphClientLoggingHandler(ILogger logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(CLIENT_REQUEST_ID_HEADER_NAME, Guid.NewGuid().ToString());
        LogHttpMessage(request.Headers, request.RequestUri!, "REQUEST");
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        LogHttpMessage(response.Headers, response.RequestMessage!.RequestUri!, "RESPONSE");
        return response;
    }

    private void LogHttpMessage(HttpHeaders headers, Uri uri, string prefix)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            StringBuilder message = new();
            message.AppendLine($"{prefix} {uri}");
            foreach (string headerName in this._headerNamesToLog)
            {
                if (headers.TryGetValues(headerName, out IEnumerable<string>? values))
                {
                    message.AppendLine($"{headerName}: {string.Join(", ", values)}");
                }
            }

            _logger.LogDebug("{0}", message);
        }
    }
}
