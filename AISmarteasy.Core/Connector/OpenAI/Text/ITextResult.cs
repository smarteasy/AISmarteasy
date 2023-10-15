namespace AISmarteasy.Core.Connector.OpenAI.Completion;

public interface ITextResult
{
    ModelResult ModelResult { get; }

    Task<string> GetCompletionAsync(CancellationToken cancellationToken = default);
}
