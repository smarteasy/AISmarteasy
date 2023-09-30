namespace SemanticKernel.Connector.Memory.Chroma;

internal sealed class ListCollectionsRequest
{
    public static ListCollectionsRequest Create()
    {
        return new ListCollectionsRequest();
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest("collections");
    }

    private ListCollectionsRequest()
    { }
}
