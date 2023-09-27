using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using SemanticKernel.Service;

namespace SemanticKernel.Connector.OpenAI.TextCompletion.Chat;

public sealed class OpenAIChatCompletion : OpenAIClientBase, IChatCompletion
{
    public OpenAIChatCompletion(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null) : base(modelId, apiKey, organization, httpClient, loggerFactory)
    {
    }


    public override async Task<ChatHistory> RunChatCompletion(ChatHistory chatHistory, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        var chatResult = await InternalGetChatResultsAsync(chatHistory, new ChatRequestSettings(), cancellationToken);
        var answer = chatResult[0].ModelResult.GetResult<ChatModelResult>().Choice.Message.Content;
        chatHistory.AddAssistantMessage(answer);
        return chatHistory;
    }
}
