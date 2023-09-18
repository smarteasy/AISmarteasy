using SemanticKernel.Connector.OpenAI.TextCompletion;

namespace SemanticKernel.Service;

public interface IAIService
{
    Task<SemanticAnswer> RunCompletion(string prompt, CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default);
}
