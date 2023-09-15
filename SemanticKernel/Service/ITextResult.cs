namespace SemanticKernel.Service;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
