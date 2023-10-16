using AISmarteasy.Core.Connector;
using AISmarteasy.Core.Connector.OpenAI.Text;

namespace AISmarteasy.Core.Service;

public interface ITextCompletion : IAIService
{
    Task<SemanticAnswer> RunTextCompletionAsync(string prompt, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);

    Task<ChatHistory> RunChatCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    ChatHistory CreateNewChat(string systemMessage);
}
