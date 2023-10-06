using System.Runtime.CompilerServices;
using AISmarteasy.Core.Connector.OpenAI.TextCompletion.Chat;
using AISmarteasy.Core.Function;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion;

public abstract class OpenAIClientBase : ClientBase
{
    private protected override OpenAIClient Client { get; }

    private protected OpenAIClientBase(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null) : base(loggerFactory)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        Verify.NotNullOrWhiteSpace(apiKey);

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

    private protected OpenAIClientBase(
       string modelId,
       OpenAIClient openAIClient,
       ILoggerFactory? loggerFactory = null) : base(loggerFactory)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        Verify.NotNull(openAIClient);

        ModelId = modelId;
        Client = openAIClient;
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

    public virtual Task<SemanticAnswer> RunTextCompletion(string prompt, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<SemanticAnswer>(null!);
    }

    public virtual Task<ChatHistory> RunChatCompletion(ChatHistory chatHistory, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ChatHistory>(null!);
    }

    public virtual Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IList<ReadOnlyMemory<float>>>(null!);
    }
}
