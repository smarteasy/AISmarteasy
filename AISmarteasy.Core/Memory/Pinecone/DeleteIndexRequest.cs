using SemanticKernel.Web;

namespace SemanticKernel.Memory.Pinecone;

internal sealed class DeleteIndexRequest
{
    public static DeleteIndexRequest Create(string indexName)
    {
        return new DeleteIndexRequest(indexName);
    }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"/databases/{_indexName}");

        request.Headers.Add("accept", "text/plain");

        return request;
    }

    private readonly string _indexName;

    private DeleteIndexRequest(string indexName)
    {
        _indexName = indexName;
    }
}
