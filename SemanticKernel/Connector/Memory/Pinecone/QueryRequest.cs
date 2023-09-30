﻿using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class QueryRequest
{
    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }

    [JsonPropertyName("topK")]
    public long TopK { get; set; }

    [JsonPropertyName("filter")]
    public Dictionary<string, object>? Filter { get; set; }

    [JsonPropertyName("vector")]
    public ReadOnlyMemory<float> Vector { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("sparseVector")]
    public SparseVectorData? SparseVector { get; set; }

    [JsonPropertyName("includeValues")]
    public bool IncludeValues { get; set; }

    [JsonPropertyName("includeMetadata")]
    public bool IncludeMetadata { get; set; }

    public static QueryRequest QueryIndex(Query query)
    {
        return new QueryRequest(query.Vector)
        {
            TopK = query.TopK,
            Filter = query.Filter,
            Namespace = query.Namespace,
            SparseVector = query.SparseVector,
            Id = query.Id
        };
    }

    public QueryRequest WithMetadata(bool includeMetadata)
    {
        this.IncludeMetadata = includeMetadata;
        return this;
    }

    public QueryRequest WithEmbeddings(bool includeValues)
    {
        this.IncludeValues = includeValues;
        return this;
    }

    public HttpRequestMessage Build()
    {
        if (this.Filter != null)
        {
            this.Filter = PineconeUtils.ConvertFilterToPineconeFilter(this.Filter);
        }

        var request = HttpRequest.CreatePostRequest(
            "/query",
            this);

        request.Headers.Add("accept", "application/json");

        return request;
    }

    [JsonConstructor]
    private QueryRequest(ReadOnlyMemory<float> values)
    {
        this.Vector = values;
    }
}