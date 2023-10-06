using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory.Pinecone;

public class MetadataIndexConfig
{
    public MetadataIndexConfig(List<string> indexed)
    {
        Indexed = indexed;
    }

    [JsonPropertyName("indexed")]
    public List<string> Indexed { get; set; }

    public static MetadataIndexConfig Default => new(new List<string>(new List<string>
    {
        "document_Id",
        "source",
        "source_Id",
        "url",
        "type",
        "tags",
        "created_at"
    }));
}
