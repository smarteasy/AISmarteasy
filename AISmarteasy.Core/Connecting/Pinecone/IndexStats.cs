﻿using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Pinecone;

public class IndexStats
{
    public IndexStats(
        Dictionary<string, IndexNamespaceStats> namespaces,
        int dimension = default,
        float indexFullness = default,
        long totalVectorCount = default)
    {
        Namespaces = namespaces;
        Dimension = dimension;
        IndexFullness = indexFullness;
        TotalVectorCount = totalVectorCount;
    }

    [JsonPropertyName("namespaces")]
    public Dictionary<string, IndexNamespaceStats> Namespaces { get; set; }

    [JsonPropertyName("dimension")]
    public int Dimension { get; set; }

    [JsonPropertyName("indexFullness")]
    public float IndexFullness { get; set; }

    [JsonPropertyName("totalVectorCount")]
    public long TotalVectorCount { get; set; }
}
