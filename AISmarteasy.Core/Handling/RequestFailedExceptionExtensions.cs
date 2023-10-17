using System.Net;
using Azure;

namespace AISmarteasy.Core.Handling;

public static class RequestFailedExceptionExtensions
{
    public static HttpOperationException ToHttpOperationException(this RequestFailedException exception)
    {
        const int NoResponseReceived = 0;
        var responseContent = exception.GetRawResponse()?.Content.ToString();
        return new HttpOperationException(
            exception.Status == NoResponseReceived ? null : (HttpStatusCode?)exception.Status,
            responseContent,
            exception.Message,
            exception);
    }
}
