using AISmarteasy.Core.Handling;

namespace AISmarteasy.Core.Connecting.Pinecone;

internal sealed class DescribeIndexRequest
{
    public string IndexName { get; }

    public static DescribeIndexRequest Create(string indexName)
    {
        return new DescribeIndexRequest(indexName);
    }

    public HttpRequestMessage Build()
    {
        var request = HttpRequest.CreateGetRequest($"/databases/{IndexName}");
        request.Headers.Add("accept", "application/json");
        return request;
    }

    private DescribeIndexRequest(string indexName)
    {
        IndexName = indexName;
    }
}
