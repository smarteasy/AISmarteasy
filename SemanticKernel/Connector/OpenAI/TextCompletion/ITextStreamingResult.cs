namespace SemanticKernel.Connector.OpenAI.TextCompletion;

public interface ITextStreamingResult : ITextResult
{
    IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default);
}
