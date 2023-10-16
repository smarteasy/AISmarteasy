namespace AISmarteasy.Core.Connector.OpenAI.Text;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
