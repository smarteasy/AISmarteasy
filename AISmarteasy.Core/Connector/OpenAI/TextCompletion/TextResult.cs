using Azure.AI.OpenAI;

namespace AISmarteasy.Core.Connector.OpenAI.TextCompletion;

internal sealed class TextResult : ITextResult
{
    private readonly Choice _choice;

    public TextResult(Completions resultData, Choice choice)
    {
        ModelResult = new ModelResult(new TextModelResult(resultData, choice));
        _choice = choice;
    }

    public ModelResult ModelResult { get; }

    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_choice.Text);
    }
}
