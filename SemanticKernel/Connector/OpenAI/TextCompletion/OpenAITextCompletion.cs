using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI.TextCompletion.Chat;
using SemanticKernel.Service;
using static System.Net.Mime.MediaTypeNames;

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
