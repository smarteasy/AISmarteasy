using Microsoft.Extensions.Logging;
using SemanticKernel.Service;

namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public sealed class OpenAITextCompletion : OpenAIClientBase, IAIService
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
    
    public override Task<SemanticAnswer> RunTextCompletion(string prompt, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return InternalGetTextResultsAsync(prompt, requestSettings, cancellationToken);
    }
}
