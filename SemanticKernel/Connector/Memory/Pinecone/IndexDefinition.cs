using System.Text;
using System.Text.Json.Serialization;
using SemanticKernel.Web;

namespace SemanticKernel.Connector.Memory.Pinecone;

public class IndexDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("metric")]
    public IndexMetric Metric { get; set; } = IndexMetric.Cosine;

    [JsonPropertyName("pod_type")]
    public PodType PodType { get; set; } = PodType.P1X1;

    [JsonPropertyName("dimension")]
    public int Dimension { get; set; } = 1536;

    [JsonPropertyName("pods")]
    public int Pods { get; set; } = 1;

    [JsonPropertyName("replicas")]
    public int Replicas { get; set; }

    [JsonPropertyName("shards")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Shards { get; set; }

    [JsonPropertyName("metadata_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataIndexConfig? MetadataConfig { get; set; }

    [JsonPropertyName("source_collection")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceCollection { get; set; }

    public static IndexDefinition Create(string name)
    {
        return new IndexDefinition(name);
    }

    public IndexDefinition WithDimension(int dimension)
    {
        Dimension = dimension;
        return this;
    }

    public IndexDefinition WithMetric(IndexMetric metric)
    {
        Metric = metric;
        return this;
    }

    public IndexDefinition NumberOfPods(int pods)
    {
        Pods = pods;
        return this;
    }

    public IndexDefinition NumberOfReplicas(int replicas)
    {
        Replicas = replicas;
        return this;
    }

    public IndexDefinition WithPodType(PodType podType)
    {
        PodType = podType;
        return this;
    }

    public IndexDefinition WithMetadataIndex(MetadataIndexConfig? config = default)
    {
        MetadataConfig = config;
        return this;
    }

    public IndexDefinition FromSourceCollection(string sourceCollection)
    {
        SourceCollection = sourceCollection;
        return this;
    }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = HttpRequest.CreatePostRequest("/databases", this);
        request.Headers.Add("accept", "text/plain");
        return request;
    }

    public static IndexDefinition Default(string? name = default)
    {
        string indexName = name ?? PineconeUtils.DefaultIndexName;

        return Create(indexName)
            .WithDimension(PineconeUtils.DefaultDimension)
            .WithMetric(PineconeUtils.DefaultIndexMetric)
            .NumberOfPods(1)
            .NumberOfReplicas(1)
            .WithPodType(PineconeUtils.DefaultPodType)
            .WithMetadataIndex(MetadataIndexConfig.Default);
    }

    public override string ToString()
    {
        StringBuilder builder = new();

        builder.Append("Configuration :");
        builder.AppendLine($"Name: {Name}, ");
        builder.AppendLine($"Dimension: {Dimension}, ");
        builder.AppendLine($"Metric: {Metric}, ");
        builder.AppendLine($"Pods: {Pods}, ");
        builder.AppendLine($"Replicas: {Replicas}, ");
        builder.AppendLine($"PodType: {PodType}, ");

        if (MetadataConfig != null)
        {
            builder.AppendLine($"MetaIndex: {string.Join(",", MetadataConfig)}, ");
        }

        if (SourceCollection != null)
        {
            builder.AppendLine($"SourceCollection: {SourceCollection}, ");
        }

        return builder.ToString();
    }

    [JsonConstructor]
    public IndexDefinition(string name)
    {
        Name = name;
    }
}
