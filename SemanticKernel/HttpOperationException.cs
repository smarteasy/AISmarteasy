using System.Net;

namespace SemanticKernel;

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
        this.StatusCode = statusCode;
        this.ResponseContent = responseContent;
    }

    public HttpStatusCode? StatusCode { get; set; }

    public string? ResponseContent { get; set; }
}
