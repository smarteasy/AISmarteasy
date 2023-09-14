namespace SemanticKernel;

/// <summary>
/// Interface for text completion streaming results.
/// Provides an asynchronous enumerable of text completion results.
/// </summary>
public interface ITextStreamingResult : ITextResult
{
    /// <summary>
    /// Gets an asynchronous enumerable of text completion results.
    /// </summary>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="string"/> representing the text completion results.</returns>
    IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default);
}
