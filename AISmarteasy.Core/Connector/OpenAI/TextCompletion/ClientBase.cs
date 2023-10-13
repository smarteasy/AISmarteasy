﻿using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Util;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion;

public abstract class ClientBase
{
    private const int MAX_RESULTS_PER_PROMPT = 128;

    private protected ClientBase(ILoggerFactory? loggerFactory = null)
    {
        Logger = loggerFactory is not null ? loggerFactory.CreateLogger(GetType()) : NullLogger.Instance;
    }

    private protected string ModelId { get; set; } = string.Empty;

    private protected abstract OpenAIClient Client { get; }

    private protected ILogger Logger { get; set; }

    private static readonly Meter Meter = new(typeof(ClientBase).Assembly.GetName().Name!);

    private static readonly Counter<int> PromptTokensCounter =
        Meter.CreateCounter<int>(
            name: "AISmarteasy.Core.Connector.OpenAI.PromptTokens",
            description: "Number of prompt tokens used");

    private static readonly Counter<int> CompletionTokensCounter =
        Meter.CreateCounter<int>(
            name: "AISmarteasy.Core.Connector.OpenAI.CompletionTokens",
            description: "Number of completion tokens used");

    private static readonly Counter<int> TotalTokensCounter =
        Meter.CreateCounter<int>(
            name: "AISmarteasy.Core.Connector.OpenAI.TotalTokens",
            description: "Total number of tokens used");

    private protected async Task<SemanticAnswer> GetTextResultsAsync(string prompt, AIRequestSettings requestSettings,
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

    private protected async Task<IReadOnlyList<IChatResult>> GetChatResultsAsync(ChatHistory chatHistory, AIRequestSettings chatSettings, 
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(chatHistory);

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

    private protected static OpenAIChatHistory CreateNewChat(string? instructions = null)
    {
        return new OpenAIChatHistory(instructions);
    }

    private protected async Task<IList<ReadOnlyMemory<float>>> InternalGetEmbeddingsAsync(IList<string> texts, CancellationToken cancellationToken = default)
    {
        var result = new List<ReadOnlyMemory<float>>(texts.Count);
        foreach (var text in texts)
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





    

    private protected async IAsyncEnumerable<TextStreamingResult> GetTextStreamingResultsAsync(string text, AIRequestSettings requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(requestSettings);

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





    private protected async IAsyncEnumerable<IChatStreamingResult> GetChatStreamingResultsAsync(
        IEnumerable<ChatMessageBase> chat,
        AIRequestSettings? requestSettings,
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

    private protected async Task<IReadOnlyList<ITextResult>> GetChatResultsAsTextAsync(string text,
        AIRequestSettings? textSettings, CancellationToken cancellationToken = default)
    {
        textSettings ??= new();
        ChatHistory chat = PrepareChatHistory(text, textSettings, out AIRequestSettings chatSettings);

        return (await GetChatResultsAsync(chat, chatSettings, cancellationToken).ConfigureAwait(false))
            .OfType<ITextResult>()
            .ToList();
    }

    private protected async IAsyncEnumerable<ITextStreamingResult> InternalGetChatStreamingResultsAsTextAsync(
        string text,
        AIRequestSettings? textSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatHistory chat = PrepareChatHistory(text, textSettings, out AIRequestSettings chatSettings);

        await foreach (var chatCompletionStreamingResult in GetChatStreamingResultsAsync(chat, chatSettings, cancellationToken).ConfigureAwait(false))
        {
            yield return (ITextStreamingResult)chatCompletionStreamingResult;
        }
    }

    private static OpenAIChatHistory PrepareChatHistory(string text, AIRequestSettings? requestSettings, out AIRequestSettings settings)
    {
        requestSettings ??= new();
        var chat = CreateNewChat(requestSettings.ChatSystemPrompt);
        chat.AddUserMessage(text);
        settings = new AIRequestSettings
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

    private static CompletionsOptions CreateCompletionsOptions(string text, AIRequestSettings requestSettings)
    {
        if (requestSettings.ResultsPerPrompt is < 1 or > MAX_RESULTS_PER_PROMPT)
        {
            throw new ArgumentOutOfRangeException($"{nameof(requestSettings)}.{nameof(requestSettings.ResultsPerPrompt)}", requestSettings.ResultsPerPrompt, $"The value must be in range between 1 and {MAX_RESULTS_PER_PROMPT}, inclusive.");
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

    private static ChatCompletionsOptions CreateChatCompletionsOptions(AIRequestSettings requestSettings, IEnumerable<ChatMessageBase> chatHistory)
    {
        if (requestSettings.ResultsPerPrompt is < 1 or > MAX_RESULTS_PER_PROMPT)
        {
            throw new ArgumentOutOfRangeException($"{nameof(requestSettings)}.{nameof(requestSettings.ResultsPerPrompt)}", requestSettings.ResultsPerPrompt, $"The value must be in range between 1 and {MAX_RESULTS_PER_PROMPT}, inclusive.");
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
        if (maxTokens is < 1)
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

        PromptTokensCounter.Add(usage.PromptTokens);
        CompletionTokensCounter.Add(usage.CompletionTokens);
        TotalTokensCounter.Add(usage.TotalTokens);
    }
}
