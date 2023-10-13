using AISmarteasy.Core.Connector;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;

namespace AISmarteasy.Core.Service;

public interface IAIService
{
    Task<SemanticAnswer> RunTextCompletion(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    Task<ChatHistory> RunChatCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, CancellationToken cancellationToken = default);
}
