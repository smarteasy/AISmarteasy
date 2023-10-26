using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connecting.OpenAI;

public sealed class OpenAIChatCompletion : OpenAIClientBase, ITextCompletion
{
    public OpenAIChatCompletion(string modelId, string apiKey, AIServiceTypeKind serviceType,
        string? organization = null, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        : base(modelId, apiKey, serviceType, organization, httpClient, loggerFactory)
    {
    }

    public IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GenerateMessageAsync(ChatHistory chat, AIRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IChatResult> chatResults = await GetChatResultsAsync(chat, requestSettings, cancellationToken).ConfigureAwait(false);
        var firstChatMessage = await chatResults[0].GetChatMessageAsync(cancellationToken).ConfigureAwait(false);
        return firstChatMessage.Content;
    }

    public IAsyncEnumerable<IChatStreamingResult> RunChatStreamingCompletionAsync(ChatHistory chatHistory, 
        AIRequestSettings requestSettings, CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return GetChatStreamingResultsAsync(chatHistory, requestSettings, cancellationToken);
    }

    public override async Task<ChatHistory> RunChatCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        var chatResult = await GetChatResultsAsync(chatHistory, requestSettings, cancellationToken).ConfigureAwait(false);
        var answer = chatResult[0].ModelResult.GetResult<ChatModelResult>().Choice.Message.Content;
        chatHistory.AddAssistantMessage(answer);
        return chatHistory;
    }
}
