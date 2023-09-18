using Microsoft.Extensions.Logging;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;

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

    public Task<SemanticAnswer> RunCompletion(
        string text,
        CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return InternalGetTextResultsAsync(text, requestSettings, cancellationToken);
    }

    //public IAsyncEnumerable<ITextStreamingResult> RunAsyncStreaming(
    //    string text,
    //    CompleteRequestSettings requestSettings,
    //    CancellationToken cancellationToken = default)
    //{
    //    LogActionDetails();
    //    return InternalGetTextStreamingResultsAsync(text, requestSettings, cancellationToken);
    //}
}
