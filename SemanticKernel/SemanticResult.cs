using Microsoft.Extensions.Logging;
using SemanticKernel.Connector.OpenAI.TextCompletion;
using SemanticKernel.Context;
using SemanticKernel.Function;
using SemanticKernel.Handler;
using SemanticKernel.Memory;
using SemanticKernel.Prompt;

namespace SemanticKernel;

public abstract class SemanticResult
{
}
public class SemanticTextResult : SemanticResult
{
    public IReadOnlyList<ITextResult>? Result { get; set; }
}
