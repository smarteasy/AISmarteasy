using System.Text;
using Azure.AI.OpenAI;

namespace AISmarteasy.Core.Connecting.OpenAI.Text;

public sealed class TextStreamingResult : ITextStreamingResult
{
    private readonly StreamingChoice _choice;

    public ModelResult ModelResult { get; }

    public TextStreamingResult(StreamingCompletions resultData, StreamingChoice choice)
    {
        ModelResult = new ModelResult(resultData);
        _choice = choice;
    }

    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        var fullMessage = new StringBuilder();
        await foreach (var message in _choice.GetTextStreaming(cancellationToken).ConfigureAwait(false))
        {
            fullMessage.Append(message);
        }

        return fullMessage.ToString();
    }

    public IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default)
    {
        return _choice.GetTextStreaming(cancellationToken);
    }
}
