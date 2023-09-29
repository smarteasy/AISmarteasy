using System.Text;
using System.Text.Json.Serialization;

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
        this.Dimension = dimension;
        return this;
    }

    public IndexDefinition WithMetric(IndexMetric metric)
    {
        this.Metric = metric;
        return this;
    }

    public IndexDefinition NumberOfPods(int pods)
    {
        this.Pods = pods;
        return this;
    }

    public IndexDefinition NumberOfReplicas(int replicas)
    {
        this.Replicas = replicas;
        return this;
    }

    public IndexDefinition WithPodType(PodType podType)
    {
        this.PodType = podType;
        return this;
    }

    public IndexDefinition WithMetadataIndex(MetadataIndexConfig? config = default)
    {
        this.MetadataConfig = config;
        return this;
    }

    public IndexDefinition FromSourceCollection(string sourceCollection)
    {
        this.SourceCollection = sourceCollection;
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
        builder.AppendLine($"Name: {this.Name}, ");
        builder.AppendLine($"Dimension: {this.Dimension}, ");
        builder.AppendLine($"Metric: {this.Metric}, ");
        builder.AppendLine($"Pods: {this.Pods}, ");
        builder.AppendLine($"Replicas: {this.Replicas}, ");
        builder.AppendLine($"PodType: {this.PodType}, ");

        if (this.MetadataConfig != null)
        {
            builder.AppendLine($"MetaIndex: {string.Join(",", this.MetadataConfig)}, ");
        }

        if (this.SourceCollection != null)
        {
            builder.AppendLine($"SourceCollection: {this.SourceCollection}, ");
        }

        return builder.ToString();
    }

    [JsonConstructor]
    public IndexDefinition(string name)
    {
        this.Name = name;
    }
}
