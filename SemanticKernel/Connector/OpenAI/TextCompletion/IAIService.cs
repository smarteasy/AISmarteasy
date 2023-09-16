namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public interface IAIService
{
    Task<SemanticResult> Run(string prompt, CompleteRequestSettings requestSettings, CancellationToken cancellationToken = default);
}
