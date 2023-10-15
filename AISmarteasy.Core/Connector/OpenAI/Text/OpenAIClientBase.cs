using System.Runtime.CompilerServices;
using System.Text;
using AISmarteasy.Core.Connector.OpenAI.Completion;
using AISmarteasy.Core.Connector.OpenAI.Completion.Chat;
using AISmarteasy.Core.Connector.OpenAI.Image;
using AISmarteasy.Core.Connector.OpenAI.Text.Chat;
using AISmarteasy.Core.Function;
using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Web;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Connector.OpenAI.Text;

public abstract class OpenAIClientBase : ClientBase
{
    private protected override OpenAIClient? Client { get; }
    private readonly ILogger? _logger;
    private readonly HttpClient? _httpClient;

    private protected OpenAIClientBase(string modelId, string apiKey, 
        string? organization = null, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        : base(loggerFactory)
    {
        Verify.NotNullOrWhitespace(modelId);
        Verify.NotNullOrWhitespace(apiKey);

        ModelId = modelId;

        var options = GetClientOptions();
        if (httpClient != null)
        {
            options.Transport = new HttpClientTransport(httpClient);
        }

        if (!string.IsNullOrWhiteSpace(organization))
        {
            options.AddPolicy(new AddHeaderRequestPolicy("OpenAI-Organization", organization), HttpPipelinePosition.PerCall);
        }

        Client = new OpenAIClient(apiKey, options);
    }

    private protected OpenAIClientBase(string modelId, OpenAIClient client, ILoggerFactory? loggerFactory = null) 
        : base(loggerFactory)
    {
        Verify.NotNullOrWhitespace(modelId);
        Verify.NotNull(client);

        ModelId = modelId;
        Client = client;
    }

    private protected OpenAIClientBase(HttpClient? httpClient, ILoggerFactory? loggerFactory = null)
    {
        _httpClient = httpClient ?? new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(this.GetType()) : NullLogger.Instance;
    }

    private protected void LogActionDetails([CallerMemberName] string? callerMemberName = default)
    {
        Logger.LogInformation("Action: {Action}. OpenAI Model ID: {ModelId}.", callerMemberName, ModelId);
    }

    private static OpenAIClientOptions GetClientOptions()
    {
        return new OpenAIClientOptions
        {
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            }
        };
    }

    public virtual Task<SemanticAnswer> RunTextCompletion(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual Task<ChatHistory> RunChatCompletionAsync(ChatHistory chatHistory, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual Task<string?> GenerateImageAsync(string description, int width, int height, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected override ChatHistory PrepareChatHistory(string text, AIRequestSettings? requestSettings, out AIRequestSettings settings)
    {
        requestSettings ??= new();
        var chatHistory = CreateNewChat(requestSettings.ChatSystemPrompt);
        chatHistory.AddUserMessage(text);
        settings = new AIRequestSettings
        {
            MaxTokens = requestSettings.MaxTokens,
            Temperature = requestSettings.Temperature,
            TopP = requestSettings.TopP,
            PresencePenalty = requestSettings.PresencePenalty,
            FrequencyPenalty = requestSettings.FrequencyPenalty,
            StopSequences = requestSettings.StopSequences,
        };
        return chatHistory;
    }

    public ChatHistory CreateNewChat(string? systemMessage = null)
    {
        return new OpenAIChatHistory(systemMessage);
    }

    private protected async Task<IList<string>?> GenerateImageGenerationAsync(string url, string requestBody, 
        Func<ImageGenerationResponse.Image, string> extractResponseFunc, CancellationToken cancellationToken = default)
    {
        var result = await ExecutePostRequestAsync<ImageGenerationResponse>(url, requestBody, cancellationToken).ConfigureAwait(false);
        return result?.Images.Select(extractResponseFunc).ToList();
    }

    private protected async Task<T?> ExecutePostRequestAsync<T>(string url, string requestBody, CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var response = await ExecuteRequestAsync(url, HttpMethod.Post, content, cancellationToken).ConfigureAwait(false);
        if (response != null)
        {
            string responseJson = await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);
            T result = JsonDeserialize<T>(responseJson);
            return result;
        }

        return default;
    }

    private protected T JsonDeserialize<T>(string responseJson)
    {
        var result = Json.Deserialize<T>(responseJson);
        if (result is null)
        {
            throw new SKException("Response JSON parse error");
        }

        return result;
    }

    private protected async Task<HttpResponseMessage?> ExecuteRequestAsync(string url, HttpMethod method, HttpContent? content, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, url);

        AddRequestHeaders(request);

        if (content != null)
        {
            request.Content = content;
        }

        if (_httpClient != null)
        {
            var response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);
            if (_logger != null)
                _logger.LogDebug("HTTP response: {0} {1}", (int)response.StatusCode, response.StatusCode.ToString("G"));
            return response;
        }


        return null;
    }

    private protected virtual void AddRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", Telemetry.HttpUserAgent);
    }
}
