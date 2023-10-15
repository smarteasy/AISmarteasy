﻿using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Connector.OpenAI.Text.Chat;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.OpenAI;

public sealed class OpenAIChatCompletion : OpenAIClientBase, IAIService
{
    public OpenAIChatCompletion(string modelId, string apiKey,
        string? organization = null, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        : base(modelId, apiKey, organization, httpClient, loggerFactory)
    {
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