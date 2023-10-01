namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class ListIndexesRequest
{
    public static ListIndexesRequest Create()
    {
        return new ListIndexesRequest();
    }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage? request = HttpRequest.CreateGetRequest("/databases");
        request.Headers.Add("accept", "application/json; charset=utf-8");
        return request;
    }
}
