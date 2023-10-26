using System.Runtime.CompilerServices;
using AISmarteasy.Core.Connecting.OpenAI.Text;
using AISmarteasy.Core.Connecting.OpenAI.Text.Chat;
using AISmarteasy.Core.PluginFunction;
using AISmarteasy.Core.Service;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connecting.OpenAI;

public sealed class OpenAITextCompletion : OpenAIClientBase, ITextCompletion
{
    public OpenAITextCompletion(string modelId, string apiKey, AIServiceTypeKind serviceType,
        string? organization = null, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null
    ) : base(modelId, apiKey, serviceType, organization, httpClient, loggerFactory)
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

        var response = await RunRequestAsync<Response<StreamingCompletions>>(
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

    public Task<string> GenerateMessageAsync(ChatHistory chat, AIRequestSettings requestSettings,
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
