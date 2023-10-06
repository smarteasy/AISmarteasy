using System.Text.Json.Serialization;
using SemanticKernel.Web;

namespace SemanticKernel.Memory.Pinecone;

internal sealed class ConfigureIndexRequest
{
    public string IndexName { get; set; }

    [JsonPropertyName("pod_type")]
    public PodType PodType { get; set; }

    [JsonPropertyName("replicas")]
    public int Replicas { get; set; }

    public static ConfigureIndexRequest Create(string indexName)
    {
        return new ConfigureIndexRequest(indexName);
    }

    public ConfigureIndexRequest WithPodType(PodType podType)
    {
        PodType = podType;
        return this;
    }

    public ConfigureIndexRequest NumberOfReplicas(int replicas)
    {
        Replicas = replicas;
        return this;
    }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage? request = HttpRequest.CreatePatchRequest(
            $"/databases/{IndexName}", this);

        request.Headers.Add("accept", "text/plain");

        return request;
    }

    private ConfigureIndexRequest(string indexName)
    {
        IndexName = indexName;
    }
}
