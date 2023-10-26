using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;

namespace AISmarteasy.Core.Service;

public interface ITextCompletion : IAIService
{
    Task<SemanticAnswer> RunTextCompletionAsync(string prompt, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);

    IAsyncEnumerable<IChatStreamingResult> RunChatStreamingCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
   
    Task<ChatHistory> RunChatCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    ChatHistory CreateNewChat(string systemMessage);

    Task<string> GenerateMessageAsync(ChatHistory chat, AIRequestSettings requestSettings, CancellationToken cancellationToken = default);
}
