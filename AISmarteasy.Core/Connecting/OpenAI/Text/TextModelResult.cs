using Azure.AI.OpenAI;

namespace AISmarteasy.Core.Connecting.OpenAI.Text;

public sealed class TextModelResult
{
    public string Id { get; }

    public DateTimeOffset Created { get; }

    public IReadOnlyList<PromptFilterResult> PromptFilterResults { get; }

    public Choice Choice { get; }

    public CompletionsUsage Usage { get; }

    internal TextModelResult(Completions completionsData, Choice choiceData)
    {
        Id = completionsData.Id;
        Created = completionsData.Created;
        PromptFilterResults = completionsData.PromptFilterResults;
        Choice = choiceData;
        Usage = completionsData.Usage;
    }
}
