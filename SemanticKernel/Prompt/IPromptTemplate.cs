using SemanticKernel.Context;
using SemanticKernel.Function;

namespace SemanticKernel.Prompt;

public interface IPromptTemplate
{
    IList<ParameterView> GetParameters();

    public Task<string> RenderAsync(SKContext context, CancellationToken cancellationToken = default);
}