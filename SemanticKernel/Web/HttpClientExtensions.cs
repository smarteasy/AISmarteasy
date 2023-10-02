using System.Net;

namespace SemanticKernel.Web;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> SendWithSuccessCheckAsync(this HttpClient client, HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
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
                responseContent = await response!.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                throw new HttpOperationException(response?.StatusCode ?? HttpStatusCode.BadRequest, responseContent, e.Message, e);
            }
        }

        return response;
    }

    public static async Task<HttpResponseMessage> SendWithSuccessCheckAsync(this HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await client.SendWithSuccessCheckAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
    }
}
