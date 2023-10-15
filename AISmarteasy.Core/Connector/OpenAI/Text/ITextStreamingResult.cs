namespace AISmarteasy.Core.Connector.OpenAI.Completion;

public interface ITextStreamingResult : ITextResult
{
    IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default);
}
