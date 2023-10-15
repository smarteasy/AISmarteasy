using AISmarteasy.Core.Connector.OpenAI.Text;
using AISmarteasy.Core.Service;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Connector.OpenAI;

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
    
    public override Task<SemanticAnswer> RunTextCompletion(string prompt, AIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        LogActionDetails();
        return GetTextResultsAsync(prompt, requestSettings, cancellationToken);
    }
}
