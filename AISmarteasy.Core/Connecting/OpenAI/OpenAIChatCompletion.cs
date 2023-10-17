using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace AISmarteasy.Core.Connecting.OpenAI;

public sealed class OpenAIChatCompletion : OpenAIClientBase, ITextCompletion
{
    public OpenAIChatCompletion(string modelId, string apiKey,
        string? organization = null, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        : base(modelId, apiKey, organization, httpClient, loggerFactory)
    {
    }

    public IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
