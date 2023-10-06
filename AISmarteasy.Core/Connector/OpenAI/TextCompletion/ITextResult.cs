namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
