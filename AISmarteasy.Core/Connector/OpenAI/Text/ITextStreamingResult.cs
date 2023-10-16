namespace AISmarteasy.Core.Connector.OpenAI.Text;

public interface ITextStreamingResult : ITextResult
{
    IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default);
}
