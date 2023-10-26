using System.ComponentModel;
using AISmarteasy.Core.Handling;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Semantic Kernel operates on strings")]
public sealed class HttpSkill
{
    private readonly HttpClient _client;

    public HttpSkill() : this(new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false))
    {
    }

    public HttpSkill(HttpClient client) =>
        this._client = client;

    [SKFunction, Description("Makes a GET request to a uri")]
    public Task<string> GetAsync(
        [Description("The URI of the request")] string uri,
        CancellationToken cancellationToken = default) =>
        SendRequestAsync(uri, HttpMethod.Get, requestContent: null, cancellationToken);

    [SKFunction, Description("Makes a POST request to a uri")]
    public Task<string> PostAsync(
        [Description("The URI of the request")] string uri,
        [Description("The body of the request")] string body,
        CancellationToken cancellationToken = default) =>
        SendRequestAsync(uri, HttpMethod.Post, new StringContent(body), cancellationToken);

    [SKFunction, Description("Makes a PUT request to a uri")]
    public Task<string> PutAsync(
        [Description("The URI of the request")] string uri,
        [Description("The body of the request")] string body,
        CancellationToken cancellationToken = default) =>
        SendRequestAsync(uri, HttpMethod.Put, new StringContent(body), cancellationToken);

    [SKFunction, Description("Makes a DELETE request to a uri")]
    public Task<string> DeleteAsync(
        [Description("The URI of the request")] string uri,
        CancellationToken cancellationToken = default) =>
        SendRequestAsync(uri, HttpMethod.Delete, requestContent: null, cancellationToken);

    private async Task<string> SendRequestAsync(string uri, HttpMethod method, HttpContent? requestContent, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Content = requestContent;
        request.Headers.Add("User-Agent", Telemetry.HTTP_USER_AGENT);
        using var response = await _client.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);
    }
}
