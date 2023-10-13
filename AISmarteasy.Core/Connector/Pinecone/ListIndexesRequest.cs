using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector.Pinecone;

internal sealed class ListIndexesRequest
{
    public static ListIndexesRequest Create()
    {
        return new ListIndexesRequest();
    }

    public HttpRequestMessage Build()
    {
        var request = HttpRequest.CreateGetRequest("/databases");
        request.Headers.Add("accept", "application/json; charset=utf-8");
        return request;
    }
}
