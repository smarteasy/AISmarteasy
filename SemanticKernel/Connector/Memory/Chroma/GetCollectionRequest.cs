﻿using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Chroma;

internal sealed class GetCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }

    public static GetCollectionRequest Create(string collectionName)
    {
        return new GetCollectionRequest(collectionName);
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"collections/{CollectionName}");
    }

    private GetCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
}
