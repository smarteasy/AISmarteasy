namespace AISmarteasy.Core.Connecting.OpenAI.Text;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
