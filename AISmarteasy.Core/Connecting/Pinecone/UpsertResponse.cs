﻿using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Pinecone;

internal sealed class UpsertResponse
{
    public UpsertResponse(int upsertedCount = default)
    {
        UpsertedCount = upsertedCount;
    }

    [JsonPropertyName("upsertedCount")]
    public int UpsertedCount { get; set; }
}
