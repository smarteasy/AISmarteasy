using System.Net;

namespace AISmarteasy.Core.Web;

public class HttpOperationException : Exception
{
    public HttpOperationException() : base()
    {
    }

    public HttpOperationException(string? message) : base(message)
    {
    }

    public HttpOperationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public HttpOperationException(HttpStatusCode? statusCode, string? responseContent, string? message, Exception? innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public HttpStatusCode? StatusCode { get; set; }

    public string? ResponseContent { get; set; }
}
