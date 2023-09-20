using SemanticKernel.Context;
using SemanticKernel.Function;

namespace SemanticKernel.Prompt;

public interface IPromptTemplate
{
    Task<string> RenderAsync(
        SKContext context,
        CancellationToken cancellationToken = default);

    IList<ParameterView> Parameters { get; }
}
