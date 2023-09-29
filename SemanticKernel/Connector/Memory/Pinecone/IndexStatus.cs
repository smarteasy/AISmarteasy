using System.Text.Json.Serialization;

namespace SemanticKernel.Connector.Memory.Pinecone;

public class IndexStatus
{
    [JsonConstructor]
    public IndexStatus(string host, int port = default, IndexState? state = default, bool ready = false)
    {
        this.Host = host;
        this.Port = port;
        this.State = state;
        this.Ready = ready;
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
