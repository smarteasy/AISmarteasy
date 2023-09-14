namespace SemanticKernel;

public interface ITextCompletion : IAIService
{
    Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(
        string text,
        CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(
        string text,
        CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = default);
}
