﻿using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Connecting.Chroma;

public class ChromaCollectionModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
