﻿using System.Runtime.CompilerServices;
using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Connector.OpenAI.Text.Chat;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Service;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;

namespace AISmarteasy.Core.Connector.OpenAI;

public sealed class OpenAITextCompletion : OpenAIClientBase, ITextCompletion
{
    public OpenAITextCompletion(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null
    ) : base(modelId, apiKey, organization, httpClient, loggerFactory)
    {
    }
    
    public override Task<SemanticAnswer> RunTextCompletionAsync(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return GetTextResultsAsync(prompt, requestSettings, cancellationToken);
    }

    public async IAsyncEnumerable<TextStreamingResult> RunTextStreamingCompletionAsync(string prompt, AIRequestSettings requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateMaxTokens(requestSettings.MaxTokens);

        var options = CreateCompletionsOptions(prompt, requestSettings);

        Response<StreamingCompletions>? response = await RunRequestAsync<Response<StreamingCompletions>>(
            () => Client?.GetCompletionsStreamingAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

        using StreamingCompletions streamingChatCompletions = response.Value;
        await foreach (StreamingChoice choice in streamingChatCompletions.GetChoicesStreaming(cancellationToken))
        {
            yield return new TextStreamingResult(streamingChatCompletions, choice);
        }
    }

    public IAsyncEnumerable<IChatStreamingResult> RunChatStreamingCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async IAsyncEnumerable<TextStreamingResult> GetTextStreamingResultsAsync(string text, AIRequestSettings requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(requestSettings);
        Verify.NotNull(Client);

        ValidateMaxTokens(requestSettings.MaxTokens);
        var options = CreateCompletionsOptions(text, requestSettings);

        var response = await RunRequestAsync<Response<StreamingCompletions>>(
            () => Client.GetCompletionsStreamingAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

        using StreamingCompletions streamingChatCompletions = response.Value;
        await foreach (StreamingChoice choice in streamingChatCompletions.GetChoicesStreaming(cancellationToken).ConfigureAwait(false))
        {
            yield return new TextStreamingResult(streamingChatCompletions, choice);
        }
    }
}
