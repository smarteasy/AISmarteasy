﻿using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

internal sealed class QueryResponse
{
    public QueryResponse(List<PineconeDocument> matches, string? nameSpace = default)
    {
        this.Matches = matches;
        this.Namespace = nameSpace;
    }

    [JsonPropertyName("matches")]
    public List<PineconeDocument> Matches { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
}
