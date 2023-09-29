﻿using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class DescribeIndexStatsRequest
{
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Filter { get; set; }

    public static DescribeIndexStatsRequest GetIndexStats()
    {
        return new DescribeIndexStatsRequest();
    }

    public DescribeIndexStatsRequest WithFilter(Dictionary<string, object>? filter)
    {
        this.Filter = filter;
        return this;
    }

    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = this.Filter == null
            ? HttpRequest.CreatePostRequest("/describe_index_stats")
            : HttpRequest.CreatePostRequest("/describe_index_stats", this);

        request.Headers.Add("accept", "application/json");

        return request;
    }

    private DescribeIndexStatsRequest()
    {
    }
}
