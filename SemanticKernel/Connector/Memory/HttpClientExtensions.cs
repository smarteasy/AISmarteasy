using System.Net;

namespace SemanticKernel.Connector.Memory;

internal static class HttpClientExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods", Justification = "The `ReadAsStringAsync` method in the NetStandard 2.0 version does not have an overload that accepts the cancellation token.")]
    internal static async Task<HttpResponseMessage> SendWithSuccessCheckAsync(this HttpClient client, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        try
        {
            response = await client.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return response;
        }
        catch (HttpRequestException e)
        {
            string? responseContent = null;

            try
            {
                responseContent = await response!.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch { } 

            throw new HttpOperationException(response?.StatusCode ?? HttpStatusCode.BadRequest, responseContent, e.Message, e);
        }
    }

    internal static async Task<HttpResponseMessage> SendWithSuccessCheckAsync(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await client.SendWithSuccessCheckAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
    }
}
