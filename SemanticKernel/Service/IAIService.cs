using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

namespace SemanticKernel.Service;

public interface IAIService
{
    Task<SemanticAnswer> RunTextCompletion(string prompt, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    Task<ChatHistory> RunChatCompletion(ChatHistory chatHistory, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, CancellationToken cancellationToken = default);
}
