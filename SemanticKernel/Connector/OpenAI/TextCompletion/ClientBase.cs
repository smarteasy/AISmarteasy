using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
using SemanticKernel.Function;
using SemanticKernel.Util;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public abstract class ClientBase
{
    private const int MaxResultsPerPrompt = 128;

    private protected ClientBase(ILoggerFactory? loggerFactory = null)
    {
        Logger = loggerFactory is not null ? loggerFactory.CreateLogger(GetType()) : NullLogger.Instance;
    }

    private protected string ModelId { get; set; } = string.Empty;

    private protected abstract OpenAIClient Client { get; }

    private protected ILogger Logger { get; set; }

    private static Meter s_meter = new(typeof(ClientBase).Assembly.GetName().Name);

    private static Counter<int> s_promptTokensCounter =
        s_meter.CreateCounter<int>(
            name: "SK.Connectors.OpenAI.PromptTokens",
            description: "Number of prompt tokens used");

    private static Counter<int> s_completionTokensCounter =
        s_meter.CreateCounter<int>(
            name: "SK.Connectors.OpenAI.CompletionTokens",
            description: "Number of completion tokens used");

    private static Counter<int> s_totalTokensCounter =
        s_meter.CreateCounter<int>(
            name: "SK.Connectors.OpenAI.TotalTokens",
            description: "Total number of tokens used");

    private protected async Task<SemanticAnswer> InternalGetTextResultsAsync(
        string prompt,
        CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(requestSettings);

        ValidateMaxTokens(requestSettings.MaxTokens);
        var options = CreateCompletionsOptions(prompt, requestSettings);

        Response<Completions>? response = await RunRequestAsync<Response<Completions>?>(
            () => Client.GetCompletionsAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

        if (response is null)
        {
            throw new SKException("Text completions null response");
        }

        var responseData = response.Value;

        if (responseData.Choices.Count == 0)
        {
            throw new SKException("Text completions not found");
        }

        CaptureUsageDetails(responseData.Usage);

        return new SemanticAnswer(responseData.Choices[0].Text);
    }

    private protected async Task<IReadOnlyList<IChatResult>> InternalGetChatResultsAsync(
        ChatHistory chatHistory,
        ChatRequestSettings? chatSettings,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(chatHistory);
        chatSettings ??= new();

        ValidateMaxTokens(chatSettings.MaxTokens);
        var chatOptions = CreateChatCompletionsOptions(chatSettings, chatHistory);

        Response<ChatCompletions>? response = await RunRequestAsync<Response<ChatCompletions>?>(
            () => Client.GetChatCompletionsAsync(ModelId, chatOptions, cancellationToken)).ConfigureAwait(false);

        if (response is null)
        {
            throw new SKException("Chat completions null response");
        }

        var responseData = response.Value;

        if (responseData.Choices.Count == 0)
        {
            throw new SKException("Chat completions not found");
        }

        CaptureUsageDetails(responseData.Usage);

        return responseData.Choices.Select(chatChoice => new ChatResult(responseData, chatChoice)).ToList();
    }

    private protected static OpenAIChatHistory InternalCreateNewChat(string? instructions = null)
    {
        return new OpenAIChatHistory(instructions);
    }



    private protected async IAsyncEnumerable<TextStreamingResult> InternalGetTextStreamingResultsAsync(
        string text,
        CompleteRequestSettings requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(requestSettings);

        ValidateMaxTokens(requestSettings.MaxTokens);
        var options = CreateCompletionsOptions(text, requestSettings);

        Response<StreamingCompletions>? response = await RunRequestAsync<Response<StreamingCompletions>>(
            () => Client.GetCompletionsStreamingAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

        using StreamingCompletions streamingChatCompletions = response.Value;
        await foreach (StreamingChoice choice in streamingChatCompletions.GetChoicesStreaming(cancellationToken))
        {
            yield return new TextStreamingResult(streamingChatCompletions, choice);
        }
    }

    private protected async Task<IList<ReadOnlyMemory<float>>> InternalGetEmbeddingsAsync(
        IList<string> data,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ReadOnlyMemory<float>>(data.Count);
        foreach (string text in data)
        {
            var options = new EmbeddingsOptions(text);

            Response<Embeddings>? response = await RunRequestAsync<Response<Embeddings>?>(
                () => Client.GetEmbeddingsAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

            if (response is null)
            {
                throw new SKException("Text embedding null response");
            }

            if (response.Value.Data.Count == 0)
            {
                throw new SKException("Text embedding not found");
            }

            result.Add(response.Value.Data[0].Embedding.ToArray());
        }

        return result;
    }



    private protected async IAsyncEnumerable<IChatStreamingResult> InternalGetChatStreamingResultsAsync(
        IEnumerable<ChatMessageBase> chat,
        ChatRequestSettings? requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(chat);
        requestSettings ??= new();

        ValidateMaxTokens(requestSettings.MaxTokens);

        var options = CreateChatCompletionsOptions(requestSettings, chat);

        Response<StreamingChatCompletions>? response = await RunRequestAsync<Response<StreamingChatCompletions>>(
            () => Client.GetChatCompletionsStreamingAsync(ModelId, options, cancellationToken)).ConfigureAwait(false);

        if (response is null)
        {
            throw new SKException("Chat completions null response");
        }

        using StreamingChatCompletions streamingChatCompletions = response.Value;
        await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming(cancellationToken).ConfigureAwait(false))
        {
            yield return new ChatStreamingResult(response.Value, choice);
        }
    }

    private protected async Task<IReadOnlyList<ITextResult>> InternalGetChatResultsAsTextAsync(
        string text,
        CompleteRequestSettings? textSettings,
        CancellationToken cancellationToken = default)
    {
        textSettings ??= new();
        ChatHistory chat = PrepareChatHistory(text, textSettings, out ChatRequestSettings chatSettings);

        return (await InternalGetChatResultsAsync(chat, chatSettings, cancellationToken).ConfigureAwait(false))
            .OfType<ITextResult>()
            .ToList();
    }

    private protected async IAsyncEnumerable<ITextStreamingResult> InternalGetChatStreamingResultsAsTextAsync(
        string text,
        CompleteRequestSettings? textSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatHistory chat = PrepareChatHistory(text, textSettings, out ChatRequestSettings chatSettings);

        await foreach (var chatCompletionStreamingResult in InternalGetChatStreamingResultsAsync(chat, chatSettings, cancellationToken))
        {
            yield return (ITextStreamingResult)chatCompletionStreamingResult;
        }
    }

    private static OpenAIChatHistory PrepareChatHistory(string text, CompleteRequestSettings? requestSettings, out ChatRequestSettings settings)
    {
        requestSettings ??= new();
        var chat = InternalCreateNewChat(requestSettings.ChatSystemPrompt);
        chat.AddUserMessage(text);
        settings = new ChatRequestSettings
        {
            MaxTokens = requestSettings.MaxTokens,
            Temperature = requestSettings.Temperature,
            TopP = requestSettings.TopP,
            PresencePenalty = requestSettings.PresencePenalty,
            FrequencyPenalty = requestSettings.FrequencyPenalty,
            StopSequences = requestSettings.StopSequences,
        };
        return chat;
    }

    private static CompletionsOptions CreateCompletionsOptions(string text, CompleteRequestSettings requestSettings)
    {
        if (requestSettings.ResultsPerPrompt is < 1 or > MaxResultsPerPrompt)
        {
            throw new ArgumentOutOfRangeException($"{nameof(requestSettings)}.{nameof(requestSettings.ResultsPerPrompt)}", requestSettings.ResultsPerPrompt, $"The value must be in range between 1 and {MaxResultsPerPrompt}, inclusive.");
        }

        var options = new CompletionsOptions
        {
            Prompts = { text.NormalizeLineEndings() },
            MaxTokens = requestSettings.MaxTokens,
            Temperature = (float?)requestSettings.Temperature,
            NucleusSamplingFactor = (float?)requestSettings.TopP,
            FrequencyPenalty = (float?)requestSettings.FrequencyPenalty,
            PresencePenalty = (float?)requestSettings.PresencePenalty,
            Echo = false,
            ChoicesPerPrompt = requestSettings.ResultsPerPrompt,
            GenerationSampleCount = requestSettings.ResultsPerPrompt,
            LogProbabilityCount = null,
            User = null,
        };

        foreach (var keyValue in requestSettings.TokenSelectionBiases)
        {
            options.TokenSelectionBiases.Add(keyValue.Key, keyValue.Value);
        }

        if (requestSettings.StopSequences is { Count: > 0 })
        {
            foreach (var s in requestSettings.StopSequences)
            {
                options.StopSequences.Add(s);
            }
        }

        return options;
    }

    private static ChatCompletionsOptions CreateChatCompletionsOptions(ChatRequestSettings requestSettings, IEnumerable<ChatMessageBase> chatHistory)
    {
        if (requestSettings.ResultsPerPrompt is < 1 or > MaxResultsPerPrompt)
        {
            throw new ArgumentOutOfRangeException($"{nameof(requestSettings)}.{nameof(requestSettings.ResultsPerPrompt)}", requestSettings.ResultsPerPrompt, $"The value must be in range between 1 and {MaxResultsPerPrompt}, inclusive.");
        }

        var options = new ChatCompletionsOptions
        {
            MaxTokens = requestSettings.MaxTokens,
            Temperature = (float?)requestSettings.Temperature,
            NucleusSamplingFactor = (float?)requestSettings.TopP,
            FrequencyPenalty = (float?)requestSettings.FrequencyPenalty,
            PresencePenalty = (float?)requestSettings.PresencePenalty,
            ChoiceCount = requestSettings.ResultsPerPrompt
        };

        foreach (var keyValue in requestSettings.TokenSelectionBiases)
        {
            options.TokenSelectionBiases.Add(keyValue.Key, keyValue.Value);
        }

        if (requestSettings.StopSequences is { Count: > 0 })
        {
            foreach (var s in requestSettings.StopSequences)
            {
                options.StopSequences.Add(s);
            }
        }

        foreach (var message in chatHistory)
        {
            var validRole = GetValidChatRole(message.Role);
            options.Messages.Add(new ChatMessage(validRole, message.Content));
        }

        return options;
    }

    private static ChatRole GetValidChatRole(AuthorRole role)
    {
        var validRole = new ChatRole(role.Label);

        if (validRole != ChatRole.User &&
            validRole != ChatRole.System &&
            validRole != ChatRole.Assistant)
        {
            throw new ArgumentException($"Invalid chat message author role: {role}");
        }

        return validRole;
    }

    private static void ValidateMaxTokens(int? maxTokens)
    {
        if (maxTokens.HasValue && maxTokens < 1)
        {
            throw new SKException($"MaxTokens {maxTokens} is not valid, the value must be greater than zero");
        }
    }

    private static async Task<T> RunRequestAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request.Invoke().ConfigureAwait(false);
        }
        catch (RequestFailedException e)
        {
            throw e.ToHttpOperationException();
        }
    }

    private void CaptureUsageDetails(CompletionsUsage usage)
    {
        Logger.LogInformation(
            "Prompt tokens: {PromptTokens}. Completion tokens: {CompletionTokens}. Total tokens: {TotalTokens}.",
            usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);

        s_promptTokensCounter.Add(usage.PromptTokens);
        s_completionTokensCounter.Add(usage.CompletionTokens);
        s_totalTokensCounter.Add(usage.TotalTokens);
    }
}
