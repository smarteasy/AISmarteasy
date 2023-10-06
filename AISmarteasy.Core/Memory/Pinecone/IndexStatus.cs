using System.Text.Json.Serialization;

namespace AISmarteasy.Core.Memory.Pinecone;

public class IndexStatus
{
    [JsonConstructor]
    public IndexStatus(string host, int port = default, IndexState? state = default, bool ready = false)
    {
        Host = host;
        Port = port;
        State = state;
        Ready = ready;
    }

    [JsonPropertyName("state")]
    public IndexState? State { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}
