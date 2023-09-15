using SemanticKernel.Context;
using SemanticKernel.Function;

namespace SemanticKernel.Prompt;

public interface IPromptTemplate
{
    IList<ParameterView> GetParameters();

    public Task<string> RenderAsync(SKContext executionContext, CancellationToken cancellationToken = default);
}
